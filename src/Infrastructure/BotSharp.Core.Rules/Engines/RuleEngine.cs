using BotSharp.Abstraction.Templating;

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

        // Resolve the criteria evaluator
        IRuleCriteriaEvaluator? criteriaEvaluator = null;
        if (options?.Criteria != null)
        {
            criteriaEvaluator = ResolveCriteriaEvaluator(options.Criteria.Mode);
            if (criteriaEvaluator == null)
            {
                _logger.LogWarning($"Unable to find rule criteria evaluator for type ({options.Criteria.Mode}).");
            }
        }

        // Trigger agents
        var filteredAgents = agents.Items.Where(x => x.Rules.Exists(r => r.TriggerName.IsEqualTo(trigger.Name) && !x.Disabled)).ToList();
        foreach (var agent in filteredAgents)
        {
            var rule = agent.Rules.FirstOrDefault(x => x.TriggerName.IsEqualTo(trigger.Name) && !x.Disabled);
            if (rule == null)
            {
                continue;
            }

            // The rule's own mode wins over the mode carried on the trigger options, so an agent can
            // pick how its criteria is judged without the caller knowing.
            var evaluator = ResolveCriteriaEvaluator(rule.Criteria?.Mode) ?? criteriaEvaluator;
            if (evaluator != null && options?.Criteria != null)
            {
                var criteriaContext = new RuleCriteriaContext
                {
                    Options = options.Criteria,
                    States = states
                };

                var isTriggered = await EvaluateCriteria(evaluator, agent, trigger, criteriaContext);
                if (!isTriggered)
                {
                    continue;
                }
            }

            var msg = !string.IsNullOrWhiteSpace(rule.Message) ? rule.Message : text;
            var convId = await SendMessageToAgent(agent, trigger, text, msg, states);
            newConversationIds.Add(convId);
        }

        return newConversationIds;
    }

    #region Criteria
    private IRuleCriteriaEvaluator? ResolveCriteriaEvaluator(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            return null;
        }

        return _services.GetServices<IRuleCriteriaEvaluator>().FirstOrDefault(x => x.Type.IsEqualTo(mode));
    }

    /// <summary>
    /// Runs the criteria evaluator. When a non-llm evaluator cannot produce an answer
    /// (null result: missing script, failed execution, error), silently fall back to the
    /// llm evaluator instead of skipping the rule. The llm evaluator is the last resort,
    /// so a null from it means "not triggered".
    /// </summary>
    private async Task<bool> EvaluateCriteria(
        IRuleCriteriaEvaluator evaluator,
        Agent agent,
        IRuleTrigger trigger,
        RuleCriteriaContext context)
    {
        var isTriggered = await evaluator.EvaluateAsync(agent, trigger, context);
        if (isTriggered != null)
        {
            return isTriggered.Value;
        }

        if (evaluator.Type.IsEqualTo(BuiltInRuleCriteria.Llm))
        {
            return false;
        }

        var llmEvaluator = ResolveCriteriaEvaluator(BuiltInRuleCriteria.Llm);
        if (llmEvaluator == null)
        {
            _logger.LogWarning($"Unable to find llm rule criteria evaluator to fall back to from ({evaluator.Type}).");
            return false;
        }

        _logger.LogInformation($"Rule criteria evaluator ({evaluator.Type}) returned no result, falling back to llm for agent ({agent.Name}) and trigger ({trigger.Name}).");
        return await llmEvaluator.EvaluateAsync(agent, trigger, context) ?? false;
    }
    #endregion

    #region Send message to agent
    private async Task<string> SendMessageToAgent(Agent agent, IRuleTrigger trigger, string title, string msg, IEnumerable<MessageState>? states = null)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        var conv = await convService.NewConversation(new Conversation
        {
            Channel = trigger.Channel,
            Title = title,
            AgentId = agent.Id
        });

        var allStates = new List<MessageState>
        {
            new("channel", trigger.Channel)
        };

        if (!states.IsNullOrEmpty())
        {
            allStates.AddRange(states!);
        }

        var message = new RoleDialogModel(AgentRole.User, RenderMessage(msg, allStates));

        await convService.SetConversationId(conv.Id, allStates);
        await convService.SendMessage(agent.Id,
            message,
            null,
            msg => Task.CompletedTask);

        await convService.SaveStates();

        return conv.Id;
    }

    private string RenderMessage(string msg, IEnumerable<MessageState> states)
    {
        if (string.IsNullOrWhiteSpace(msg))
        {
            return msg;
        }

        try
        {
            var data = new Dictionary<string, object>();
            foreach (var state in states)
            {
                if (string.IsNullOrEmpty(state.Key))
                {
                    continue;
                }

                data[state.Key] = state.Value;
            }

            var render = _services.GetRequiredService<ITemplateRender>();
            return render.Render(msg, data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Unable to render the rule message template, falling back to the raw message ({msg}).");
            return msg;
        }
    }
    #endregion
}
