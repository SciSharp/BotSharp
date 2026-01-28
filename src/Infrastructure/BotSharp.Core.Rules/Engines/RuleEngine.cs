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

    public async Task<IEnumerable<string>> Triggered(IRuleTrigger trigger, string text, IEnumerable<MessageState>? states = null, RuleTriggerOptions? options = null)
    {
        var newConversationIds = new List<string>();

        // Pull all user defined rules
        var agentService = _services.GetRequiredService<IAgentService>();
        var agents = await agentService.GetAgents(options?.AgentFilter ?? new AgentFilter
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
            var rule = agent.Rules.FirstOrDefault(x => x.TriggerName.IsEqualTo(trigger.Name) && !x.Disabled);
            if (rule == null)
            {
                continue;
            }

            // Criteria validation
            if (!string.IsNullOrEmpty(rule.RuleCriteria?.Name) && !rule.RuleCriteria.Disabled)
            {
                var criteriaResult = await ExecuteCriteriaAsync(agent, rule, trigger, rule.RuleCriteria?.Name, text, states);
                if (criteriaResult?.IsValid == false)
                {
                    _logger.LogWarning("Criteria validation failed for agent {AgentId} with trigger {TriggerName}", agent.Id, trigger.Name);
                    continue;
                }
            }
            
            // Execute action
            if (rule.RuleAction?.Disabled == true)
            {
                continue; 
            }

            var actionName = rule?.RuleAction?.Name ?? RuleConstant.DEFAULT_ACTION_NAME;
            var actionResult = await ExecuteActionAsync(agent, rule, trigger, actionName, text, states);
            if (actionResult?.Success == true && !string.IsNullOrEmpty(actionResult.ConversationId))
            {
                newConversationIds.Add(actionResult.ConversationId);
            }
        }

        return newConversationIds;
    }


    #region Criteria
    private async Task<RuleCriteriaResult> ExecuteCriteriaAsync(
        Agent agent,
        AgentRule rule,
        IRuleTrigger trigger,
        string? criteriaProvider,
        string text,
        IEnumerable<MessageState>? states)
    {
        var result = new RuleCriteriaResult();

        try
        {
            var criteria = _services.GetServices<IRuleCriteria>()
                                    .FirstOrDefault(x => x.Provider == criteriaProvider);

            if (criteria == null)
            {
                return result;
            }


            var context = new RuleCriteriaContext
            {
                Text = text,
                Parameters = BuildContextParameters(rule.RuleCriteria?.Config, states)
            };

            _logger.LogInformation("Start execution rule criteria {CriteriaProvider} for agent {AgentId} with trigger {TriggerName}",
                criteria.Provider, agent.Id, trigger.Name);

            var hooks = _services.GetHooks<IRuleTriggerHook>(agent.Id);
            foreach (var hook in hooks)
            {
                await hook.BeforeRuleCriteriaExecuted(agent, trigger, context);
            }

            // Execute criteria
            context.Parameters ??= [];
            result = await criteria.ValidateAsync(agent, trigger, context);

            foreach (var hook in hooks)
            {
                await hook.AfterRuleCriteriaExecuted(agent, trigger, result);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing rule criteria {CriteriaProvider} for agent {AgentId}", criteriaProvider ?? string.Empty, agent.Id);
            return result;
        }
    }
    #endregion


    #region Action
    private async Task<RuleActionResult> ExecuteActionAsync(
        Agent agent,
        AgentRule rule,
        IRuleTrigger trigger,
        string? actionName,
        string text,
        IEnumerable<MessageState>? states)
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

            var context = new RuleActionContext
            {
                Text = text,
                Parameters = BuildContextParameters(rule.RuleAction?.Config, states)
            };

            _logger.LogInformation("Start execution rule action {ActionName} for agent {AgentId} with trigger {TriggerName}",
                action.Name, agent.Id, trigger.Name);

            var hooks = _services.GetHooks<IRuleTriggerHook>(agent.Id);
            foreach (var hook in hooks)
            {
                await hook.BeforeRuleActionExecuted(agent, trigger, context);
            }

            // Execute action
            context.Parameters ??= [];
            var result = await action.ExecuteAsync(agent, trigger, context);

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
    #endregion


    #region Private methods
    private Dictionary<string, object?> BuildContextParameters(JsonDocument? config, IEnumerable<MessageState>? states)
    {
        var dict = new Dictionary<string, object?>();

        if (config != null)
        {
            dict = ConvertToDictionary(config);
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

    private static Dictionary<string, object?> ConvertToDictionary(JsonDocument doc)
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
        #endregion
    }
}
