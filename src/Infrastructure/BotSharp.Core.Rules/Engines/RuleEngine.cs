using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Coding;
using BotSharp.Abstraction.Coding.Enums;
using BotSharp.Abstraction.Coding.Settings;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Hooks;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Rules.Options;
using BotSharp.Abstraction.Utilities;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.Json;

namespace BotSharp.Core.Rules.Engines;

public class RuleEngine : IRuleEngine
{
    private readonly IServiceProvider _services;
    private readonly ILogger<RuleEngine> _logger;
    private readonly CodingSettings _codingSettings;

    public RuleEngine(
        IServiceProvider services,
        ILogger<RuleEngine> logger,
        CodingSettings codingSettings)
    {
        _services = services;
        _logger = logger;
        _codingSettings = codingSettings;
    }

    public async Task<IEnumerable<string>> Triggered(IRuleTrigger trigger, string text, IEnumerable<MessageState>? states = null, RuleTriggerOptions? options = null)
    {
        var newConversationIds = new List<string>();

        // Pull all user defined rules
        var agentService = _services.GetRequiredService<IAgentService>();
        var agents = await agentService.GetAgents(new AgentFilter
        {
            Pager = new Pagination
            {
                Size = 1000
            }
        });

        // Trigger agents
        var filteredAgents = agents.Items.Where(x => x.Rules.Exists(r => r.TriggerName.IsEqualTo(trigger.Name) && !x.Disabled)).ToList();
        foreach (var agent in filteredAgents)
        {
            bool? isTriggered = true;

            // Code trigger
            if (options != null)
            {
                isTriggered = await TriggerCodeScript(agent.Id, trigger.Name, options);
            }

            if (isTriggered != null && !isTriggered.Value)
            {
                continue;
            }

            var convService = _services.GetRequiredService<IConversationService>();
            var conv = await convService.NewConversation(new Conversation
            {
                Channel = trigger.Channel,
                Title = text,
                AgentId = agent.Id
            });

            var message = new RoleDialogModel(AgentRole.User, text);

            var allStates = new List<MessageState>
            {
                new("channel", trigger.Channel)
            };

            if (states != null)
            {
                allStates.AddRange(states);
            }

            convService.SetConversationId(conv.Id, allStates);

            await convService.SendMessage(agent.Id,
                message,
                null,
                msg => Task.CompletedTask);

            convService.SaveStates();
            newConversationIds.Add(conv.Id);
        }

        return newConversationIds;
    }

    #region Private methods
    private async Task<bool?> TriggerCodeScript(string agentId, string triggerName, RuleTriggerOptions options)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return false;
        }

        var provider = options.CodeProcessor ?? BuiltInCodeProcessor.PyInterpreter;
        var processor = _services.GetServices<ICodeProcessor>().FirstOrDefault(x => x.Provider.IsEqualTo(provider));
        if (processor == null)
        {
            _logger.LogWarning($"Unable to find code processor: {provider}.");
            return false;
        }

        var agentService = _services.GetRequiredService<IAgentService>();
        var scriptName = options.CodeScriptName ?? $"{triggerName}_rule.py";
        var codeScript = await agentService.GetAgentCodeScript(agentId, scriptName, scriptType: AgentCodeScriptType.Src);

        var msg = $"rule trigger ({triggerName}) code script ({scriptName}) in agent ({agentId}) => args: {options.ArgumentContent?.RootElement.GetRawText()}.";

        if (string.IsNullOrWhiteSpace(codeScript?.Content))
        {
            _logger.LogWarning($"Unable to find {msg}.");
            return null;
        }

        try
        {
            var hooks = _services.GetHooks<IInstructHook>(agentId);

            var (useLock, useProcess, timeoutSeconds) = GetCodeExecutionConfig();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            var response = await processor.RunAsync(codeScript.Content, options: new()
            {
                ScriptName = scriptName,
                Arguments = BuildArguments(options.ArgumentName, options.ArgumentContent),
                UseLock = useLock,
                UseProcess = useProcess
            }, cancellationToken: cts.Token);

            


            if (response == null || !response.Success)
            {
                _logger.LogWarning($"Failed to handle {msg}");
                return false;
            }

            bool result;
            LogLevel logLevel;
            if (response.Result.IsEqualTo("true"))
            {
                logLevel = LogLevel.Information;
                result = true;
            }
            else
            {
                logLevel = LogLevel.Warning;
                result = false;
            }

            _logger.Log(logLevel, $"Code script execution result ({response.Result}) from {msg}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when handling {msg}");
            return false;
        }
    }

    private IEnumerable<KeyValue> BuildArguments(string? name, JsonDocument? args)
    {
        var keyValues = new List<KeyValue>();
        if (args != null)
        {
            keyValues.Add(new KeyValue(name ?? "trigger_args", args.RootElement.GetRawText()));
        }
        return keyValues;
    }

    private (bool, bool, int) GetCodeExecutionConfig()
    {
        var useLock = false;
        var useProcess = false;
        var timeoutSeconds = 3;

        useLock = _codingSettings.CodeExecution?.UseLock ?? useLock;
        useProcess = _codingSettings.CodeExecution?.UseProcess ?? useProcess;
        timeoutSeconds = _codingSettings.CodeExecution?.TimeoutSeconds > 0 ? _codingSettings.CodeExecution.TimeoutSeconds.Value : timeoutSeconds;

        return (useLock, useProcess, timeoutSeconds);
    }
    #endregion
}
