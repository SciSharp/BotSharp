using BotSharp.Abstraction.Coding;
using BotSharp.Abstraction.Coding.Enums;
using BotSharp.Abstraction.Conversations;
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

    public RuleEngine(
        IServiceProvider services,
        ILogger<RuleEngine> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<IEnumerable<string>> Trigger(IRuleTrigger trigger, string text, RuleTriggerOptions? options = null)
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

        var isTriggered = true;

        // Code trigger
        if (options != null)
        {
            isTriggered = await TriggerCodeScript(trigger.Name, options);
        }

        if (!isTriggered)
        {
            return newConversationIds;
        }

        // Trigger agents
        var filteredAgents = agents.Items.Where(x => x.Rules.Exists(r => r.TriggerName == trigger.Name && !x.Disabled)).ToList();
        foreach (var agent in filteredAgents)
        {
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

            if (options?.States != null)
            {
                allStates.AddRange(options.States);
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
    private async Task<bool> TriggerCodeScript(string triggerName, RuleTriggerOptions options)
    {
        var agentId = options.AgentId;
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

        var msg = $"rule trigger ({triggerName}) code script ({scriptName}) in agent ({agentId}) => args: {options.Arguments?.RootElement.GetRawText()}.";

        if (string.IsNullOrWhiteSpace(codeScript))
        {
            _logger.LogWarning($"Unable to find {msg}.");
            return false;
        }

        try
        {
            var response = await processor.RunAsync(codeScript, options: new()
            {
                ScriptName = scriptName,
                Arguments = BuildArguments(options.Arguments, options.States)
            });

            if (response == null || !response.Success)
            {
                _logger.LogWarning($"Failed to handle {msg}");
                return false;
            }

            bool result;
            LogLevel logLevel;
            if (response.Result.IsEqualTo("true") || response.Result.IsEqualTo("1"))
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

    private IEnumerable<KeyValue> BuildArguments(JsonDocument? args, IEnumerable<MessageState>? states)
    {
        var dict = new Dictionary<string, string>();
        if (!states.IsNullOrEmpty())
        {
            foreach (var state in states)
            {
                if (state.Value != null)
                {
                    dict[state.Key] = state.Value.ConvertToString();
                }
            }
        }

        if (args != null)
        {
            dict["trigger_args"] = args.RootElement.GetRawText();
        }

        return dict.Select(x => new KeyValue(x.Key, x.Value));
    }
#endregion
}
