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
                States = states
            };
            var result = await ExecuteActionAsync(agent, trigger, foundRule.Action.IfNullOrEmptyAs("Chat")!, context);
            if (result.Success && !string.IsNullOrEmpty(result.ConversationId))
            {
                newConversationIds.Add(result.ConversationId);
            }
        }

        return newConversationIds;
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

            return await action.ExecuteAsync(agent, trigger, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing rule action {ActionName} for agent {AgentId}", actionName, agent.Id);
            return RuleActionResult.Failed(ex.Message);
        }
    }
}
