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

            var ruleConfig = rule.Config;
            var ruleFlowProvider = options?.Flow?.TopologyProvider ?? ruleConfig?.TopologyProvider;

            if (!string.IsNullOrEmpty(ruleFlowProvider))
            {
                // Execute graph
                // 1. Load graph
                var graph = await LoadGraph(ruleFlowProvider, agent, trigger, options?.Flow);
                if (graph == null)
                {
                    continue;
                }

                // 2. Get root node
                var param = options?.Flow?.Parameters;
                var rootNodeName = param != null ? param.GetValueOrDefault("root_node_name")?.ToString() : null;
                var root = graph.GetRootNode(rootNodeName);
                if (root == null)
                {
                    graph.Clear();
                    continue;
                }

                // 3. Execute graph
                var execResults = new List<RuleFlowStepResult>();
                await ExecuteGraphNode(root, graph, agent, trigger, text, states, null, options, execResults);
                graph.Clear();

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
            Flow = options.Flow,
            JsonOptions = options.JsonOptions
        };

        var execResults = new List<RuleFlowStepResult>();
        await ExecuteGraphNode(
            node, graph,
            agent, trigger,
            options.Text,
            options.States,
            null,
            triggerOptions,
            execResults);
        graph.Clear();
    }

    #region Graph
    private async Task<RuleGraph?> LoadGraph(string provider, Agent agent, IRuleTrigger trigger, RuleFlowOptions? options)
    {
        var flow = _services.GetServices<IRuleFlow<RuleGraph>>().FirstOrDefault(x => x.Provider.IsEqualTo(provider));
        if (flow == null)
        {
            return null;
        }

        var param = new Dictionary<string, object>(options?.Parameters ?? []);
        param["agent"] = param.GetValueOrDefault("agent", agent.Name);
        param["agent_id"] = param.GetValueOrDefault("agent_id", agent.Id);
        param["trigger"] = param.GetValueOrDefault("trigger", trigger.Name);

        var topologyId = options?.TopologyId;
        if (string.IsNullOrEmpty(topologyId))
        {
            var config = await flow.GetTopologyConfigAsync();
            topologyId = config.TopologyId;
        }

        return await flow.GetTopologyAsync(topologyId, options: new()
        {
            AgentId = agent.Id,
            TriggerName = trigger.Name,
            Query = options?.Query,
            Parameters = param
        });
    }

    private async Task ExecuteGraphNode(
        RuleNode node,
        RuleGraph graph,
        Agent agent,
        IRuleTrigger trigger,
        string text,
        IEnumerable<MessageState>? states,
        Dictionary<string, string?>? data,
        RuleTriggerOptions? options,
        List<RuleFlowStepResult> results)
    {
        if (options?.Flow?.TraversalAlgorithm?.IsEqualTo("bfs") == true)
        {
            await ExecuteGraphNodeBfs(node, graph, agent, trigger, text, states, data, options, results);
        }
        else
        {
            await ExecuteGraphNodeDfs(node, graph, agent, trigger, text, states, data, options, results);
        }
    }

    private async Task ExecuteGraphNodeDfs(
        RuleNode node,
        RuleGraph graph,
        Agent agent,
        IRuleTrigger trigger,
        string text,
        IEnumerable<MessageState>? states,
        Dictionary<string, string?>? data,
        RuleTriggerOptions? options,
        List<RuleFlowStepResult> results)
    {
        // Check whether the action nodes have been visited more than limit
        var visited = results.Count();
        var param = options?.Flow?.Parameters ?? [];
        var maxRecursion = int.TryParse(param.GetValueOrDefault("max_recursion")?.ToString(), out var depth) && depth > 0
            ? depth : RuleConstant.MAX_GRAPH_RECURSION;

        var innerData = new Dictionary<string, string?>(data ?? []);

        if (visited >= maxRecursion)
        {
            _logger.LogWarning("Exceed max graph recursion {MaxRecursion} (agent {Agent} and trigger {Trigger}).",
                maxRecursion, agent.Name, trigger.Name);
            return;
        }

        // Get current node successors
        var nextNodes = graph.GetChildrenNodes(node);
        if (nextNodes.IsNullOrEmpty())
        {
            return;
        }

        // Visit neighbor nodes
        foreach (var (nextNode, edge) in nextNodes)
        {
            // Build context
            var context = new RuleFlowContext
            {
                Node = nextNode,
                Edge = edge,
                Graph = graph,
                Text = text,
                Parameters = BuildParameters(nextNode.Config, states, innerData),
                PrevStepResults = results,
                JsonOptions = options?.JsonOptions
            };

            if (RuleConstant.CONDITION_NODE_TYPES.Contains(nextNode.Type))
            {
                // Execute condition node
                var conditionResult = await ExecuteCondition(nextNode, graph, agent, trigger, context);
                if (conditionResult == null)
                {
                    results.Add(RuleFlowStepResult.FromResult(new()
                    {
                        Success = false,
                        ErrorMessage = $"Unable to find condition {nextNode.Name}."
                    }, nextNode));
                    continue;
                }

                results.Add(RuleFlowStepResult.FromResult(conditionResult, nextNode));

                // If condition result is true, then execute the next node, otherwise skip
                if (conditionResult.Success)
                {
                    await ExecuteGraphNodeDfs(nextNode, graph, agent, trigger, text, states, context.Parameters, options, results);
                }
                else
                {
                    _logger.LogInformation("Condition {ConditionName} evaluated to false, skipping next node (agent {Agent} and trigger {Trigger}).",
                        nextNode.Name, agent.Name, trigger.Name);
                }
            }
            else if (RuleConstant.ACTION_NODE_TYPES.Contains(nextNode.Type))
            {
                // Execute action node
                var actionResult = await ExecuteAction(nextNode, graph, agent, trigger, context);
                if (actionResult == null)
                {
                    results.Add(RuleFlowStepResult.FromResult(new()
                    {
                        Success = false,
                        ErrorMessage = $"Unable to find action {nextNode.Name}."
                    }, nextNode));
                    continue;
                }

                results.Add(RuleFlowStepResult.FromResult(actionResult, nextNode));

                if (actionResult.IsDelayed)
                {
                    continue;
                }

                await ExecuteGraphNodeDfs(nextNode, graph, agent, trigger, text, states, context.Parameters, options, results);
            }
            else
            {
                results.Add(RuleFlowStepResult.FromResult(new()
                {
                    Success = true,
                    Response = $"Pass through node {nextNode.Name}."
                }, nextNode));
                await ExecuteGraphNodeDfs(nextNode, graph, agent, trigger, text, states, context.Parameters, options, results);
            }
        }
    }

    private async Task ExecuteGraphNodeBfs(
        RuleNode root,
        RuleGraph graph,
        Agent agent,
        IRuleTrigger trigger,
        string text,
        IEnumerable<MessageState>? states,
        Dictionary<string, string?>? data,
        RuleTriggerOptions? options,
        List<RuleFlowStepResult> results)
    {
        var param = options?.Flow?.Parameters ?? [];
        var maxRecursion = int.TryParse(param.GetValueOrDefault("max_recursion")?.ToString(), out var depth) && depth > 0
            ? depth : RuleConstant.MAX_GRAPH_RECURSION;

        var innerData = new Dictionary<string, string?>(data ?? []);

        // Each queue entry is (node-to-process, edge-that-leads-to-it)
        var queue = new Queue<(RuleNode Node, RuleEdge Edge)>();

        foreach (var (childNode, edge) in graph.GetChildrenNodes(root))
        {
            queue.Enqueue((childNode, edge));
        }

        while (queue.Count > 0)
        {
            if (results.Count >= maxRecursion)
            {
                _logger.LogWarning("Exceed max graph nodes {MaxNodes} during BFS (agent {Agent} and trigger {Trigger}).",
                    maxRecursion, agent.Name, trigger.Name);
                break;
            }

            var (nextNode, nextEdge) = queue.Dequeue();

            var context = new RuleFlowContext
            {
                Node = nextNode,
                Edge = nextEdge,
                Graph = graph,
                Text = text,
                Parameters = BuildParameters(nextNode.Config, states, innerData),
                PrevStepResults = results,
                JsonOptions = options?.JsonOptions
            };

            if (RuleConstant.CONDITION_NODE_TYPES.Contains(nextNode.Type))
            {
                // Execute condition node
                var conditionResult = await ExecuteCondition(nextNode, graph, agent, trigger, context);
                innerData = new(context.Parameters ?? []);

                if (conditionResult == null)
                {
                    results.Add(RuleFlowStepResult.FromResult(new()
                    {
                        Success = false,
                        ErrorMessage = $"Unable to find condition {nextNode.Name}."
                    }, nextNode));
                    continue;
                }

                results.Add(RuleFlowStepResult.FromResult(conditionResult, nextNode));

                // If condition is true, enqueue children; otherwise skip the branch
                if (conditionResult.Success)
                {
                    foreach (var (childNode, childEdge) in graph.GetChildrenNodes(nextNode))
                    {
                        queue.Enqueue((childNode, childEdge));
                    }
                }
                else
                {
                    _logger.LogInformation("Condition {ConditionName} evaluated to false, skipping next node (agent {Agent} and trigger {Trigger}).",
                        nextNode.Name, agent.Name, trigger.Name);
                }
            }
            else if (RuleConstant.ACTION_NODE_TYPES.Contains(nextNode.Type))
            {
                // Execute action node
                var actionResult = await ExecuteAction(nextNode, graph, agent, trigger, context);
                innerData = new(context.Parameters ?? []);

                if (actionResult == null)
                {
                    results.Add(RuleFlowStepResult.FromResult(new()
                    {
                        Success = false,
                        ErrorMessage = $"Unable to find action {nextNode.Name}."
                    }, nextNode));
                    continue;
                }

                results.Add(RuleFlowStepResult.FromResult(actionResult, nextNode));

                if (!actionResult.IsDelayed)
                {
                    foreach (var (childNode, childEdge) in graph.GetChildrenNodes(nextNode))
                    {
                        queue.Enqueue((childNode, childEdge));
                    }
                }
            }
            else
            {
                results.Add(RuleFlowStepResult.FromResult(new()
                {
                    Success = true,
                    Response = $"Pass through node {nextNode.Name}."
                }, nextNode));

                foreach (var (childNode, childEdge) in graph.GetChildrenNodes(nextNode))
                {
                    queue.Enqueue((childNode, childEdge));
                }
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
                await hook.BeforeRuleActionExecuting(agent, node, trigger, context);
            }

            // Execute action
            context.Parameters ??= [];
            var result = await foundAction.ExecuteAsync(agent, trigger, context);

            foreach (var hook in hooks)
            {
                await hook.AfterRuleActionExecuted(agent, node, trigger, context, result);
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
                await hook.BeforeRuleConditionExecuting(agent, node, trigger, context);
            }

            // Execute condition
            context.Parameters ??= [];
            var result = await foundCondition.EvaluateAsync(agent, trigger, context);

            foreach (var hook in hooks)
            {
                await hook.AfterRuleConditionExecuted(agent, node, trigger, context, result);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing rule condition {ConditionName} for agent {AgentId}", node?.Name, agent.Id);
            return new RuleNodeResult
            {
                Success = false,
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
    private Dictionary<string, string?> BuildParameters(
        Dictionary<string, string?>? config,
        IEnumerable<MessageState>? states,
        Dictionary<string, string?>? param = null)
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

        if (!param.IsNullOrEmpty())
        {
            foreach (var pair in param!)
            {
                dict[pair.Key] = pair.Value;
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
