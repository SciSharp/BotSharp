using BotSharp.Abstraction.Rules.Hooks;
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
            // Criteria validation
            if (options?.Criteria != null)
            {
                var criteria = _services.GetServices<IRuleCriteria>()
                                        .FirstOrDefault(x => x.Provider == (options?.Criteria?.Provider ?? "botsharp-rule-criteria"));

                if (criteria == null)
                {
                    _logger.LogWarning("No criteria provider found for {Provider}, skipping agent {AgentId}", options.Criteria.Provider, agent.Id);
                    continue;
                }

                var isValid = await criteria.ValidateAsync(agent, trigger, options.Criteria);
                if (!isValid)
                {
                    _logger.LogDebug("Criteria validation failed for agent {AgentId} with trigger {TriggerName}", agent.Id, trigger.Name);
                    continue;
                }
            }

            var foundRule = agent.Rules.FirstOrDefault(x => x.TriggerName.IsEqualTo(trigger.Name) && !x.Disabled);
            if (foundRule == null)
            {
                continue;
            }

            var context = new RuleActionContext
            {
                Text = text,
                States = BuildRuleActionContext(foundRule, states)
            };
            var result = await ExecuteActionAsync(agent, trigger, foundRule.Action.IfNullOrEmptyAs("BotSharp-chat")!, context);
            if (result.Success && !string.IsNullOrEmpty(result.ConversationId))
            {
                newConversationIds.Add(result.ConversationId);
            }
        }

        return newConversationIds;
    }

    private Dictionary<string, object?> BuildRuleActionContext(AgentRule rule, IEnumerable<MessageState>? states)
    {
        var dict = new Dictionary<string, object?>();

        if (rule.ActionConfig != null)
        {
            dict = ConvertToDictionary(rule.ActionConfig);
        }
        
        if (!states.IsNullOrEmpty())
        {
            foreach (var state in states!)
            {
                dict[state.Key] = state.Value;
            }
        }
        
        return dict;
    }

    private async Task<RuleActionResult> ExecuteActionAsync(
        Agent agent,
        IRuleTrigger trigger,
        string actionName,
        RuleActionContext context)
    {
        try
        {
            // Get all registered rule actions
            var actions = _services.GetServices<IRuleAction>();

            // Find the matching action
            var action = actions.FirstOrDefault(x => x.Name.IsEqualTo(actionName));

            if (action == null)
            {
                var errorMsg = $"No rule action {actionName} is found";
                _logger.LogWarning(errorMsg);
                return RuleActionResult.Failed(errorMsg);
            }

            _logger.LogInformation("Start execution rule action {ActionName} for agent {AgentId} with trigger {TriggerName}",
                action.Name, agent.Id, trigger.Name);

            // Combine states
            

            var hooks = _services.GetHooks<IRuleTriggerHook>(agent.Id);
            foreach (var hook in hooks)
            {
                await hook.BeforeRuleActionExecuted(agent, trigger, context);
            }

            // Execute action
            var result =  await action.ExecuteAsync(agent, trigger, context);

            foreach (var hook in hooks)
            {
                await hook.AfterRuleActionExecuted(agent, trigger, result);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing rule action {ActionName} for agent {AgentId}", actionName, agent.Id);
            return RuleActionResult.Failed(ex.Message);
        }
    }

    public static Dictionary<string, object?> ConvertToDictionary(JsonDocument doc)
    {
        var dict = new Dictionary<string, object?>();

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            dict[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString(),
                JsonValueKind.Number when prop.Value.TryGetInt32(out int intValue) => intValue,
                JsonValueKind.Number when prop.Value.TryGetInt64(out long longValue) => longValue,
                JsonValueKind.Number when prop.Value.TryGetDouble(out double doubleValue) => doubleValue,
                JsonValueKind.Number when prop.Value.TryGetDecimal(out decimal decimalValue) => decimalValue,
                JsonValueKind.Number when prop.Value.TryGetByte(out byte byteValue) => byteValue,
                JsonValueKind.Number when prop.Value.TryGetSByte(out sbyte sbyteValue) => sbyteValue,
                JsonValueKind.Number when prop.Value.TryGetUInt16(out ushort uint16Value) => uint16Value,
                JsonValueKind.Number when prop.Value.TryGetUInt32(out uint uint32Value) => uint32Value,
                JsonValueKind.Number when prop.Value.TryGetUInt64(out ulong uint64Value) => uint64Value,
                JsonValueKind.Number when prop.Value.TryGetDateTime(out DateTime dateTimeValue) => dateTimeValue,
                JsonValueKind.Number when prop.Value.TryGetDateTimeOffset(out DateTimeOffset dateTimeOffsetValue) => dateTimeOffsetValue,
                JsonValueKind.Number when prop.Value.TryGetGuid(out Guid guidValue) => guidValue,
                JsonValueKind.Number => prop.Value.GetRawText(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Undefined => null,
                JsonValueKind.Array => prop.Value,
                JsonValueKind.Object => prop.Value,
                _ => prop.Value
            };
        }

        return dict;
    }
}
