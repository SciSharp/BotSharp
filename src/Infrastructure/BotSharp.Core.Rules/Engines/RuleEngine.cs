using System.Data;

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
            // Criteria
            if (options?.Criteria != null)
            {
                var criteria = _services.GetServices<IRuleCriteria>()
                                        .FirstOrDefault(x => x.Provider == (options?.Criteria?.Provider ?? RuleHandler.DefaultProvider));

                if (criteria == null)
                {
                    continue;
                }

                var isTriggered = await criteria.ExecuteCriteriaAsync(agent, trigger.Name, options.Criteria);
                if (!isTriggered)
                {
                    continue;
                }
            }

            var foundTrigger = agent.Rules.FirstOrDefault(x => x.TriggerName.IsEqualTo(trigger.Name) && !x.Disabled);
            if (foundTrigger == null)
            {
                continue;
            }

            var action = _services.GetServices<IRuleAction>()
                                  .FirstOrDefault(x => x.Provider == (options?.Action?.Provider ?? RuleHandler.DefaultProvider));
            if (action == null)
            {
                continue;
            }

            // Execute action
            if (foundTrigger.Action.IsEqualTo(RuleActionType.Method))
            {
                if (options?.Action?.Method?.Func != null)
                {
                    await action.ExecuteMethodAsync(agent, options.Action.Method.Func);
                }
            }
            else if (foundTrigger.Action.IsEqualTo(RuleActionType.EventMessage))
            {
                await action.SendEventMessageAsync(foundTrigger.Delay, options?.Action?.EventMessage);
            }
            else if (foundTrigger.Action.IsEqualTo(RuleActionType.Http))
            {
                
            }
            else
            {
                var conversationId = await action.SendChatAsync(agent, payload: new()
                {
                    Text = text,
                    Channel = trigger.Channel,
                    States = states
                });

                if (!string.IsNullOrEmpty(conversationId))
                {
                    newConversationIds.Add(conversationId);
                }
            }
        }

        return newConversationIds;
    }
}
