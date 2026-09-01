using BotSharp.Abstraction.MessageHub.Models;
using BotSharp.Abstraction.MessageHub.Services;
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

    public async Task<IEnumerable<string>> Triggered(IRuleTrigger trigger, string text, IEnumerable<MessageState>? states = null, RuleTriggerOptions? options = null, CancellationToken cancellationToken = default)
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

        // Flatten the agent/rule pairs so they can be throttled as one unit, rather than
        // running one agent's rules concurrently but the agents themselves one at a time.
        var pendingRules = agents.Items
            .Where(x => !x.Disabled)
            .SelectMany(x => x.Rules
                .Where(r => r != null && r.TriggerName.IsEqualTo(trigger.Name) && !r.Disabled)
                .Select(r => (Agent: x, Rule: r)))
            .ToList();

        if (pendingRules.IsNullOrEmpty())
        {
            return newConversationIds;
        }

        // Indexed so the returned conversation ids keep the rule order regardless of
        // which run finishes first.
        var convIds = new string?[pendingRules.Count];

        // Per-call options win over the configured setting, which in turn wins over the built-in default.
        var settings = _services.GetService<RuleSettings>();
        var maxConcurrency = options?.MaxConcurrency
            ?? settings?.MaxConcurrency
            ?? RuleTriggerOptions.DefaultMaxConcurrency;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, maxConcurrency),
            CancellationToken = cancellationToken
        };

        var indexedRules = pendingRules.Select((item, index) => (item.Agent, item.Rule, Index: index));

        try
        {
            await Parallel.ForEachAsync(indexedRules, parallelOptions, async (item, token) =>
            {
                try
                {
                    convIds[item.Index] = await RunRule(item.Agent, item.Rule, trigger, text, states, options, token);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // One misbehaving rule should not take down the rules that run alongside it.
                    _logger.LogError(ex, $"Error when running rule ({item.Rule.TriggerName}) for agent ({item.Agent.Name}).");
                }
            });
        }
        catch (OperationCanceledException ex)
        {
            // Cancellation still surfaces to the caller, but the conversations that were already
            // started ride along on the exception so they are not silently lost.
            var startedIds = CollectConversationIds(convIds);
            _logger.LogWarning($"Rule trigger ({trigger.Name}) was cancelled after starting {startedIds.Count} conversation(s).");
            throw new RuleTriggerCanceledException(startedIds, cancellationToken, ex);
        }

        newConversationIds.AddRange(CollectConversationIds(convIds));
        return newConversationIds;
    }

    private static List<string> CollectConversationIds(string?[] convIds)
        => convIds.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).ToList();

    /// <summary>
    /// Evaluates one rule and, when it is triggered, sends its message to the agent.
    /// Returns the new conversation id, or null when the rule did not trigger.
    /// </summary>
    private async Task<string?> RunRule(
        Agent agent,
        AgentRule rule,
        IRuleTrigger trigger,
        string text,
        IEnumerable<MessageState>? states,
        RuleTriggerOptions? options,
        CancellationToken cancellationToken)
    {
        // Every rule runs in its own scope so the scoped conversation, state and routing
        // services start clean per run, concurrent rules cannot bleed into each other, and
        // the caller's own scope is left untouched.
        using var scope = _services.CreateScope();
        var sp = scope.ServiceProvider;

        // The rule's own mode wins over the mode carried on the trigger options, so an agent can
        // pick how its criteria is judged without the caller knowing.
        var evaluator = ResolveCriteriaEvaluator(sp, rule.CriteriaConfig?.Mode)
            ?? ResolveCriteriaEvaluator(sp, options?.Criteria?.Mode);

        if (evaluator == null && !string.IsNullOrWhiteSpace(options?.Criteria?.Mode))
        {
            _logger.LogWarning($"Unable to find rule criteria evaluator for type ({options.Criteria.Mode}).");
        }

        if (evaluator != null && options?.Criteria != null)
        {
            var criteriaContext = new RuleCriteriaContext
            {
                Options = options.Criteria,
                States = states
            };

            var isTriggered = await EvaluateCriteria(sp, evaluator, agent, rule, trigger, criteriaContext);
            if (!isTriggered)
            {
                return null;
            }
        }

        // Criteria evaluation can be slow (the llm evaluator calls out), so re-check before
        // starting a conversation that nobody is waiting on any more.
        cancellationToken.ThrowIfCancellationRequested();

        var msg = !string.IsNullOrWhiteSpace(rule.Message) ? rule.Message : text;
        var convId = await SendMessageToAgent(sp, agent, trigger, text, msg, states);

        // Hold the concurrency slot a little longer after sending, so a large batch of rules
        // does not hammer the downstream provider the moment each slot frees up.
        var delay = options?.SendMessageDelayMs ?? RuleTriggerOptions.DefaultSendMessageDelayMs;
        if (delay > 0)
        {
            await Task.Delay(delay, cancellationToken);
        }

        return convId;
    }

    #region Criteria
    private IRuleCriteriaEvaluator? ResolveCriteriaEvaluator(IServiceProvider sp, string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            return null;
        }

        return sp.GetServices<IRuleCriteriaEvaluator>().FirstOrDefault(x => x.Type.IsEqualTo(mode));
    }

    /// <summary>
    /// Runs the criteria evaluator. When a non-llm evaluator cannot produce an answer
    /// (null result: missing script, failed execution, error), silently fall back to the
    /// llm evaluator instead of skipping the rule. The llm evaluator is the last resort,
    /// so a null from it means "not triggered".
    /// </summary>
    private async Task<bool> EvaluateCriteria(
        IServiceProvider sp,
        IRuleCriteriaEvaluator evaluator,
        Agent agent,
        AgentRule agentRule,
        IRuleTrigger trigger,
        RuleCriteriaContext context)
    {
        var isTriggered = await evaluator.EvaluateAsync(agent, agentRule, trigger, context);
        if (isTriggered != null)
        {
            return isTriggered.Value;
        }

        if (evaluator.Type.IsEqualTo(BuiltInRuleCriteria.Llm))
        {
            return false;
        }

        var llmEvaluator = ResolveCriteriaEvaluator(sp, BuiltInRuleCriteria.Llm);
        if (llmEvaluator == null)
        {
            _logger.LogWarning($"Unable to find llm rule criteria evaluator to fall back to from ({evaluator.Type}).");
            return false;
        }

        _logger.LogInformation($"Rule criteria evaluator ({evaluator.Type}) returned no result, falling back to llm for agent ({agent.Name}) and trigger ({trigger.Name}).");
        return await llmEvaluator.EvaluateAsync(agent, agentRule, trigger, context) ?? false;
    }
    #endregion

    #region Send message to agent
    private async Task<string> SendMessageToAgent(IServiceProvider sp, Agent agent, IRuleTrigger trigger, string title, string msg, IEnumerable<MessageState>? states = null)
    {
        var convService = sp.GetRequiredService<IConversationService>();
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

        var message = new RoleDialogModel(AgentRole.User, RenderMessage(sp, msg, allStates));

        // Subscribe the message hub observers so the rule-triggered conversation emits the same
        // events (streaming, indications, etc.) as a user-initiated one.
        var observer = sp.GetRequiredService<IObserverService>();
        using var container = observer.SubscribeObservers<HubObserveData<RoleDialogModel>>(conv.Id);

        await convService.SetConversationId(conv.Id, allStates);
        await convService.SendMessage(agent.Id,
            message,
            null,
            msg => Task.CompletedTask);

        await convService.SaveStates();
        return conv.Id;
    }

    private string RenderMessage(IServiceProvider sp, string msg, IEnumerable<MessageState> states)
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

            var render = sp.GetRequiredService<ITemplateRender>();
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
