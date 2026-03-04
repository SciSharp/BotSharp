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

            if (!string.IsNullOrEmpty(options?.GraphOptions?.Provider)
                && !string.IsNullOrEmpty(options?.GraphOptions?.GraphId))
            {
                // Execute graph
                // 1. Load graph
                var graph = await LoadGraph(options.GraphOptions.Provider, options.GraphOptions.GraphId, agent.Id, trigger, states);
                if (graph == null)
                {
                    continue;
                }

                // 2. Get root node
                var root = graph.GetRootNode(options.GraphOptions.RootNodeName);
                if (root == null)
                {
                    continue;
                }

                // 3. Execute graph
                var execResults = new List<RuleFlowStepResult>();
                await ExecuteGraphNode(root, graph, agent, trigger, text, states, options, execResults);

                // Get conversation id to support legacy features
                var convIds = execResults.Where(x => x.Success && x.Data.TryGetValue("conversation_id", out _))
                                         .Select(x => x.Data.GetValueOrDefault("conversation_id", string.Empty))
                                         .Where(x => !string.IsNullOrEmpty(x))
                                         .ToList();

                newConversationIds.AddRange(convIds);
            }
            else
            {
                var convId = await SendMessageToAgent(agent, trigger, text, states);
                newConversationIds.Add(convId);
            }
        }

        return newConversationIds;
    }

    public async Task ExecuteGraphNode(RuleNode node, RuleGraph graph, string agentId, IRuleTrigger trigger, RuleNodeExecutionOptions options)
    {
        if (node == null || graph == null || options == null)
        {
            return;
        }

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.GetAgent(agentId);

        var triggerOptions = new RuleTriggerOptions
        {
            GraphOptions = options.GraphOptions
        };

        var execResults = new List<RuleFlowStepResult>();
        await ExecuteGraphNode(
            node, graph,
            agent, trigger,
            options.Text,
            options.States,
            triggerOptions,
            execResults);
    }

    #region Graph
    private async Task<RuleGraph?> LoadGraph(string provider, string graphId, string agentId, IRuleTrigger trigger, IEnumerable<MessageState>? states)
    {
        var graph = _services.GetServices<IRuleGraph>().FirstOrDefault(x => x.Provider.IsEqualTo(provider));
        if (graph == null)
        {
            return null;
        }

        return await graph.LoadGraphAsync(graphId, options: new()
        {
            AgentId = agentId,
            Trigger = trigger.Name,
            States = states
        });
    }

    private async Task ExecuteGraphNode(
        RuleNode node,
        RuleGraph graph,
        Agent agent,
        IRuleTrigger trigger,
        string text,
        IEnumerable<MessageState>? states,
        RuleTriggerOptions? options,
        List<RuleFlowStepResult> results)
    {
        var actionResultCount = results.Count(x => RuleConstant.ACTION_NODE_TYPES.Contains(x.Node.Type));
        var maxRecursion = options?.GraphOptions?.MaxGraphRecursion ?? RuleConstant.MAX_GRAPH_RECURSION;

        if (actionResultCount >= maxRecursion)
        {
            _logger.LogWarning("Exceed max graph recursion {MaxRecursion} (agent {Agent} and trigger {Trigger}).",
                maxRecursion, agent.Name, trigger.Name);
            return;
        }

        var neighbors = graph.GetNeighbors(node);
        foreach (var (neighborNode, edge) in neighbors)
        {
            if (RuleConstant.END_NODE_TYPES.Contains(neighborNode.Type))
            {
                continue;
            }

            // Build context
            var context = new RuleFlowContext
            {
                Node = neighborNode,
                Graph = graph,
                Text = text,
                Parameters = BuildContextParameters(neighborNode.Config, states),
                PrevStepResults = results,
                JsonOptions = options?.JsonOptions
            };


            if (RuleConstant.CONDITION_NODE_TYPES.Contains(neighborNode.Type))
            {
                // Execute condition
                var conditionResult = await ExecuteCondition(neighborNode, graph, agent, trigger, context);
                if (conditionResult == null)
                {
                    continue;
                }

                results.Add(RuleFlowStepResult.FromResult(conditionResult, neighborNode));

                // If condition result is true, then execute the next node, otherwise skip
                if (conditionResult.IsValid)
                {
                    await ExecuteGraphNode(neighborNode, graph, agent, trigger, text, states, options, results);
                }
                else
                {
                    _logger.LogInformation("Condition {ConditionName} evaluated to false, skipping next node (agent {Agent} and trigger {Trigger}).",
                        neighborNode.Name, agent.Name, trigger.Name);
                }
            }
            else if (RuleConstant.ACTION_NODE_TYPES.Contains(neighborNode.Type))
            {
                // Execute action
                var actionResult = await ExecuteAction(neighborNode, graph, agent, trigger, context);
                if (actionResult == null)
                {
                    continue;
                }

                results.Add(RuleFlowStepResult.FromResult(actionResult, neighborNode));

                if (actionResult.IsDelayed)
                {
                    continue;
                }

                actionResultCount = results.Count(x => RuleConstant.ACTION_NODE_TYPES.Contains(x.Node.Type));
                if (actionResultCount >= maxRecursion)
                {
                    _logger.LogWarning("Exceed max graph recursion {MaxRecursion} (agent {Agent} and trigger {Trigger}).",
                                        maxRecursion, agent.Name, trigger.Name);
                    break;
                }

                await ExecuteGraphNode(neighborNode, graph, agent, trigger, text, states, options, results);
            }
        }
    }
    #endregion

    #region Action
    private async Task<RuleNodeResult?> ExecuteAction(
        RuleNode node,
        RuleGraph graph,
        Agent agent,
        IRuleTrigger trigger,
        RuleFlowContext context)
    {
        try
        {
            // Find the matching action
            var foundAction = GetRuleAction(node, agent, trigger);
            if (foundAction == null)
            {
                var errorMsg = $"No rule action {node?.Name} is found";
                _logger.LogWarning(errorMsg);
                return null;
            }

            _logger.LogInformation("Start execution rule action {ActionName} for agent {AgentId} with trigger {TriggerName}",
                foundAction.Name, agent.Id, trigger.Name);

            var hooks = _services.GetHooks<IRuleTriggerHook>(agent.Id);
            foreach (var hook in hooks)
            {
                await hook.BeforeRuleActionExecuted(agent, node, trigger, context);
            }

            // Execute action
            context.Parameters ??= [];
            var result = await foundAction.ExecuteAsync(agent, trigger, context);

            foreach (var hook in hooks)
            {
                await hook.AfterRuleActionExecuted(agent, node, trigger, result);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing rule action {ActionName} for agent {AgentId}", node?.Name, agent.Id);
            return new RuleNodeResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    // Find the matching action
    private IRuleAction? GetRuleAction(RuleNode node, Agent agent, IRuleTrigger trigger)
    {
        var actions = _services.GetServices<IRuleAction>()
                               .Where(x => x.Name.IsEqualTo(node?.Name))
                               .ToList();

        var found = actions.FirstOrDefault(x => !string.IsNullOrEmpty(x.AgentId) && x.AgentId.IsEqualTo(agent.Id) && x.Triggers?.Contains(trigger.Name) == true);
        if (found != null)
        {
            return found;
        }

        found = actions.FirstOrDefault(x => !string.IsNullOrEmpty(x.AgentId) && x.AgentId.IsEqualTo(agent.Id));
        if (found != null)
        {
            return found;
        }

        found = actions.FirstOrDefault(x => x.Triggers?.Contains(trigger.Name, StringComparer.OrdinalIgnoreCase) == true);
        if (found != null)
        {
            return found;
        }

        found = actions.FirstOrDefault();
        if (found != null)
        {
            return found;
        }

        return null;
    }
    #endregion


    #region Condition
    private async Task<RuleNodeResult?> ExecuteCondition(
        RuleNode node,
        RuleGraph graph,
        Agent agent,
        IRuleTrigger trigger,
        RuleFlowContext context)
    {
        try
        {
            // Find the matching condition
            var foundCondition = GetRuleCondition(node, agent, trigger);
            if (foundCondition == null)
            {
                var errorMsg = $"No rule condition {node?.Name} is found";
                _logger.LogWarning(errorMsg);
                return null;
            }

            _logger.LogInformation("Start execution rule condition {ConditionName} for agent {AgentId} with trigger {TriggerName}",
                foundCondition.Name, agent.Id, trigger.Name);

            var hooks = _services.GetHooks<IRuleTriggerHook>(agent.Id);
            foreach (var hook in hooks)
            {
                await hook.BeforeRuleConditionExecuted(agent, node, trigger, context);
            }

            // Execute condition
            context.Parameters ??= [];
            var result = await foundCondition.EvaluateAsync(agent, trigger, context);

            foreach (var hook in hooks)
            {
                await hook.AfterRuleConditionExecuted(agent, node, trigger, result);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing rule condition {ConditionName} for agent {AgentId}", node?.Name, agent.Id);
            return new RuleNodeResult
            {
                Success = false,
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
    }

    // Find the matching condition
    private IRuleCondition? GetRuleCondition(RuleNode node, Agent agent, IRuleTrigger trigger)
    {
        var conditions = _services.GetServices<IRuleCondition>()
                               .Where(x => x.Name.IsEqualTo(node?.Name))
                               .ToList();

        var found = conditions.FirstOrDefault(x => !string.IsNullOrEmpty(x.AgentId) && x.AgentId.IsEqualTo(agent.Id) && x.Triggers?.Contains(trigger.Name) == true);
        if (found != null)
        {
            return found;
        }

        found = conditions.FirstOrDefault(x => !string.IsNullOrEmpty(x.AgentId) && x.AgentId.IsEqualTo(agent.Id));
        if (found != null)
        {
            return found;
        }

        found = conditions.FirstOrDefault(x => x.Triggers?.Contains(trigger.Name, StringComparer.OrdinalIgnoreCase) == true);
        if (found != null)
        {
            return found;
        }

        found = conditions.FirstOrDefault();
        if (found != null)
        {
            return found;
        }

        return null;
    }
    #endregion


    #region Private methods
    private Dictionary<string, string?> BuildContextParameters(Dictionary<string, string?>? config, IEnumerable<MessageState>? states)
    {
        var dict = new Dictionary<string, string?>();

        if (config != null)
        {
            dict = new(config);
        }

        if (!states.IsNullOrEmpty())
        {
            foreach (var state in states!)
            {
                dict[state.Key] = state.Value?.ConvertToString();
            }
        }

        return dict;
    }
    #endregion

    #region Legacy conversation
    private async Task<string> SendMessageToAgent(Agent agent, IRuleTrigger trigger, string text, IEnumerable<MessageState>? states = null)
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

        if (!states.IsNullOrEmpty())
        {
            allStates.AddRange(states!);
        }

        await convService.SetConversationId(conv.Id, allStates);
        await convService.SendMessage(agent.Id,
            message,
            null,
            msg => Task.CompletedTask);

        await convService.SaveStates();

        return conv.Id;
    }
    #endregion
}
