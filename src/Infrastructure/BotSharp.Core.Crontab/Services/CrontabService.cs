/*****************************************************************************
  Copyright 2024 Written by Haiping Chen. All Rights Reserved.

  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at

      http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
******************************************************************************/

using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Infrastructures;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Tasks;
using BotSharp.Abstraction.Tasks.Models;
using BotSharp.Abstraction.Utilities;
using BotSharp.Core.Infrastructures;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace BotSharp.Core.Crontab.Services;

/// <summary>
/// The Crontab service schedules distributed events based on the execution times provided by users. 
/// In a scalable environment, distributed locks are used to ensure that each event is triggered only once.
/// </summary>
public class CrontabService : ICrontabService, ITaskFeeder
{
    private readonly IServiceProvider _services;
    private ILogger _logger;

    public CrontabService(IServiceProvider services, ILogger<CrontabService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<List<CrontabItem>> GetCrontable()
    {
        var repo = _services.GetRequiredService<IBotSharpRepository>();
        var crontable = await repo.GetCrontabItems(CrontabItemFilter.Empty());

        // Add fixed crontab items from cronsources
        var fixedCrontabItems = crontable.Items.ToList();
        var cronSources = _services.GetServices<ICrontabSource>();
        foreach (var source in cronSources)
        {
            if (source.IsRealTime)
            {
                continue;
            }
            var item = source.GetCrontabItem();
            fixedCrontabItems.Add(item);
        }

        return fixedCrontabItems;
    }

    public async Task<List<AgentTask>> GetTasks()
    {
        var tasks = new List<AgentTask>();
        var agentService = _services.GetRequiredService<IAgentService>();
        var cronsources = _services.GetServices<ICrontabSource>();

        // Get all agent subscribed to this cron
        var agents = await agentService.GetAgents(new AgentFilter
        {
            Pager = new Pagination
            {
                Size = 1000
            }
        });

        foreach (var source in cronsources)
        {
            var cron = source.GetCrontabItem();
            var preFilteredAgents = agents.Items.Where(x =>
                x.Rules.Exists(r => r.TriggerName == cron.Title)).ToList();

            tasks.AddRange(preFilteredAgents.Select(x => new AgentTask
            {
                Id = Guid.Empty.ToString(),
                AgentId = x.Id,
                Agent = new Agent
                {
                    Name = x.Name,
                    Description = x.Description
                },
                Name = FormatCrontabName(cron.Title, x.Name),
                Content = $"Trigger: {cron.Title}\r\nAgent: {x.Name}\r\nCron expression: {cron.Cron}",
                Status = TaskStatus.Scheduled,
                Enabled = !x.Disabled,
                Description = cron.Description,
                LastExecutionTime = cron.LastExecutionTime
            }));
        }

        return tasks;
    }

    private string FormatCrontabName(string trigger, string agent)
    {
        trigger = trigger.Replace("RuleTrigger", string.Empty);
        trigger = Regex.Replace(trigger, "(?<!^)([A-Z])", " $1");
        agent = agent.Replace("Operator", string.Empty);
        return $"{trigger}";
    }

    public async Task ScheduledTimeArrived(CrontabItem item)
    {
        _logger.LogDebug($"ScheduledTimeArrived {item}");

        await HookEmitter.Emit<ICrontabHook>(_services, async hook =>
        {
            if (hook.Triggers == null || hook.Triggers.Contains(item.Title))
            {
                hook.OnAuthenticate(item);
                await hook.OnTaskExecuting(item);
                await hook.OnCronTriggered(item);
                await hook.OnTaskExecuted(item);
            }
        }, item.AgentId);
    }

    public async Task ExecuteTimeArrivedItemWithReentryProtection(CrontabItem item)
    {
        if (!item.ReentryProtection)
        {
            await ExecuteTimeArrivedItem(item);
            return;
        }

        var lockKey = $"crontab:execution:{item.Title}";
        using var scope = _services.CreateScope();
        var locker = scope.ServiceProvider.GetRequiredService<IDistributedLocker>();
        var acquired = false;
        var lockAcquired = false;

        try
        {
            acquired = await locker.LockAsync(lockKey, async () =>
            {
                lockAcquired = true;
                _logger.LogInformation("Crontab: {0}, Distributed lock acquired, beginning execution...", item.Title);
                await ExecuteTimeArrivedItem(item);
            }, timeout: 600);

            if (!acquired)
            {
                _logger.LogWarning("Crontab: {0}, Failed to acquire distributed lock, task is still executing, skipping this occurrence to prevent re-entry.", item.Title);
            }
        }
        catch (Exception ex)
        {
            if (!lockAcquired)
            {
                _logger.LogWarning("Crontab: {0}, Redis exception occurred before acquiring lock: {1}, executing without lock protection (re-entry protection disabled).", item.Title, ex.Message);
                await ExecuteTimeArrivedItem(item);
            }
            else
            {
                _logger.LogWarning("Crontab: {0}, Redis exception occurred after lock acquired: {1}, task execution completed but lock release failed.", item.Title, ex.Message);
            }
        }
    } 
    
    private async Task<bool> ExecuteTimeArrivedItem(CrontabItem item)
    {
        try
        {
            _logger.LogInformation($"Start running crontab {item.Title}");
            await ScheduledTimeArrived(item);
            _logger.LogInformation($"Complete running crontab {item.Title}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when running crontab {item.Title}");
            return false;
        }
    }
}
