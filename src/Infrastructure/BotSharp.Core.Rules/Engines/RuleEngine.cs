using BotSharp.Abstraction.MessageHub.Models;
using BotSharp.Abstraction.Infrastructures.Enums;
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

        // Flatten the agent/rule pairs so the two loops read as one sequence of rules.
        var pendingRules = agents.Items
            .Where(x => !x.Disabled)
            .SelectMany(x => x.Rules
                .Where(r => r != null && r.TriggerName.IsEqualTo(trigger.Name) && !r.Disabled)
                .Select(r => (Agent: x, Rule: r)))
            .ToList();

        foreach (var item in pendingRules)
        {
            // A cancellation source of the rule's own, so a rule that runs too long is the only thing given
            // up on and the loop still gets to the rules behind it. Linked to the caller's token so a
            // cancelled run cuts the rule in flight short the way it did before there was a per-rule limit;
            // which of the two fired is what the catch clauses below tell apart. Null when no limit is
            // configured, which is the default - then there is nothing to cancel but the caller's token.
            using var ruleCts = CreateRuleCancellation(options, cancellationToken);

            try
            {
                var convId = await RunRule(item.Agent, item.Rule, trigger, text, states, options, ruleCts?.Token ?? cancellationToken);
                if (!string.IsNullOrEmpty(convId))
                {
                    newConversationIds.Add(convId);
                }
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                // The caller cancelled the run, so nothing further is dispatched. The conversations that
                // were already started ride along on the exception so they are not silently lost.
                _logger.LogWarning($"Rule trigger ({trigger.Name}) was cancelled after starting {newConversationIds.Count} conversation(s).");
                throw new RuleTriggerCanceledException(newConversationIds.ToList(), cancellationToken, ex);
            }
            catch (OperationCanceledException ex)
            {
                // The run itself was not cancelled, so this is the rule's own limit running out - or
                // something inside it timing out on its own account. Either way it is one rule's problem,
                // and the rules that follow still get their turn, each under a source of its own.
                _logger.LogError(ex, $"Rule ({item.Rule.TriggerName}) for agent ({item.Agent.Name}) did not finish before it was cancelled, moving on to the next rule.");
            }
            catch (Exception ex)
            {
                // One misbehaving rule should not take down the rules that follow it.
                _logger.LogError(ex, $"Error when running rule ({item.Rule.TriggerName}) for agent ({item.Agent.Name}).");
            }
        }

        return newConversationIds;
    }

    /// <summary>
    /// The cancellation source a single rule runs under.
    /// </summary>
    /// <returns>
    /// Null when no per-rule limit is configured, which is the default - the caller's own token is then all
    /// there is to run under, and there is no source to dispose. A limit of zero or less is read the same
    /// way, so <see cref="Timeout.InfiniteTimeSpan"/> and null say the same thing.
    /// </returns>
    private static CancellationTokenSource? CreateRuleCancellation(RuleTriggerOptions? options, CancellationToken cancellationToken)
    {
        var timeout = options?.RuleTimeout;
        if (timeout == null || timeout <= TimeSpan.Zero)
        {
            return null;
        }

        // Linked rather than standalone: a cancelled run should still reach the rule in flight, and the
        // caller's token is checked first when the exception comes back, so a latched one is read as
        // "stop the run" rather than being mistaken for this rule's limit.
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout.Value);
        return cts;
    }

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
        var convId = await SendMessageToAgent(sp, agent, trigger, text, msg, states, options);

        // Pause before the next rule, so a large batch does not hammer the downstream provider.
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
    private async Task<string> SendMessageToAgent(IServiceProvider sp, Agent agent, IRuleTrigger trigger, string title, string msg, IEnumerable<MessageState>? states = null, RuleTriggerOptions? options = null)
    {
        var convService = sp.GetRequiredService<IConversationService>();
        var conv = await convService.NewConversation(new Conversation
        {
            Channel = trigger.Channel,
            Title = title,
            AgentId = agent.Id
        });

        // Reported here rather than on the way out: everything below can throw, and the conversation
        // already exists by this point, so a caller that only saw the returned ids would be left with a
        // conversation nothing points at. Awaited so the caller has finished recording it before the parts
        // that can fail run.
        await NotifyConversationCreated(options, conv.Id);

        var allStates = new List<MessageState>
        {
            new(StateConst.CHANNEL, trigger.Channel)
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

    /// <summary>
    /// Tells the caller a conversation was created, if it asked to be told.
    /// </summary>
    /// <remarks>
    /// Failures are swallowed on purpose: the callback is a caller's bookkeeping, and letting it cost the
    /// rule the run it is in the middle of would be the worse outcome of the two.
    /// </remarks>
    private async Task NotifyConversationCreated(RuleTriggerOptions? options, string conversationId)
    {
        if (options?.OnConversationCreated == null || string.IsNullOrEmpty(conversationId))
        {
            return;
        }

        try
        {
            await options.OnConversationCreated(conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error when reporting the created conversation ({conversationId}) back to the rule trigger caller.");
        }
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
