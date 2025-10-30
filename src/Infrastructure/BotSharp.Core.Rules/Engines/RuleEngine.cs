using BotSharp.Abstraction.Coding;
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

        // Apply code trigger
        if (options != null
            && !string.IsNullOrWhiteSpace(options.CodeProcessor)
            && !string.IsNullOrWhiteSpace(options.AgentId))
        {
            var scriptName = options.CodeScriptName ?? $"{trigger.Name}_cron.py";
            isTriggered = await HandleCodeTrigger(options.AgentId, scriptName, options.CodeProcessor, trigger.Name, options.Arguments, options.States);
        }

        if (!isTriggered)
        {
            return newConversationIds;
        }

        var filteredAgents = agents.Items.Where(x => x.Rules.Exists(r => r.TriggerName == trigger.Name && !x.Disabled)).ToList();

        // Trigger agents
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

            /*foreach (var rule in agent.Rules)
            {
                var userSay = $"===Input data with Before and After values===\r\n{data}\r\n\r\n===Trigger Criteria===\r\n{rule.Criteria}\r\n\r\nJust output 1 or 0 without explanation: ";

                var result = await instructService.Execute(BuiltInAgentId.RulesInterpreter, new RoleDialogModel(AgentRole.User, userSay), "criteria_check", "#TEMPLATE#");

                // Check if meet the criteria
                if (result.Text == "1")
                {
                    // Hit rule
                    _logger.LogInformation($"Hit rule {rule.TriggerName} {rule.EntityType} {rule.EventName}, {data}");

                    await convService.SendMessage(agent.Id, 
                        new RoleDialogModel(AgentRole.User, $"The conversation was triggered by {rule.Criteria}"), 
                        null, 
                        msg => Task.CompletedTask);
                }
            }*/
        }

        return newConversationIds;
    }

    #region Private methods
    private async Task<bool> HandleCodeTrigger(string agentId, string scriptName, string codeProcessor, string triggerName, JsonDocument? args = null, IEnumerable<MessageState>? states = null)
    {
        var processor = _services.GetServices<ICodeProcessor>().FirstOrDefault(x => x.Provider.IsEqualTo(codeProcessor));
        if (processor == null)
        {
            _logger.LogWarning($"Unable to find code processor: {codeProcessor}.");
            return false;
        }

        var agentService = _services.GetRequiredService<IAgentService>();
        var codeScript = await agentService.GetAgentCodeScript(agentId, scriptName, scriptType: AgentCodeScriptType.Src);

        if (string.IsNullOrWhiteSpace(codeScript))
        {
            _logger.LogWarning($"Unable to find code script ({scriptName}) in agent ({agentId}).");
            return false;
        }

        var msg = $"rule trigger ({triggerName}) code script ({scriptName}) in agent ({agentId}) => args: {args?.RootElement.GetRawText()}.";

        try
        {
            var response = await processor.RunAsync(codeScript, options: new()
            {
                ScriptName = scriptName,
                Arguments = BuildArguments(args, states)
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

            _logger.Log(logLevel, $"Code result: {response.Result}. {msg}");
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
            dict["trigger_input"] = args.RootElement.GetRawText();
        }

        return dict.Select(x => new KeyValue(x.Key, x.Value));
    }
#endregion
}
