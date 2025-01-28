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

using BotSharp.Abstraction.Repositories;
using BotSharp.Core.Infrastructures;
using Microsoft.Extensions.Logging;

namespace BotSharp.Core.Crontab.Services;

/// <summary>
/// The Crontab service schedules distributed events based on the execution times provided by users. 
/// In a scalable environment, distributed locks are used to ensure that each event is triggered only once.
/// </summary>
public class CrontabService : ICrontabService
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
        var crontable = repo.GetCrontabItems(CrontabItemFilter.Empty());

        // Add fixed crontab items from cronsources
        var fixedCrantabItems = crontable.Items.ToList();
        var cronsources = _services.GetServices<ICrontabSource>();
        foreach (var source in cronsources)
        {
            var item = source.GetCrontabItem();
            fixedCrantabItems.Add(source.GetCrontabItem());
        }

        return fixedCrantabItems;
    }

    public async Task ScheduledTimeArrived(CrontabItem item)
    {
        _logger.LogDebug($"ScheduledTimeArrived {item}");
        
        await HookEmitter.Emit<ICrontabHook>(_services, async hook =>
        {
            if (hook.Triggers == null || hook.Triggers.Contains(item.Title))
            {
                await hook.OnTaskExecuting(item);
                await hook.OnCronTriggered(item);
                await hook.OnTaskExecuted(item);
            }
        });
    }
}
