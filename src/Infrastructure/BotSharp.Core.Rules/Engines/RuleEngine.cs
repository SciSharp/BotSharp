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
            var ruleFlowTopologyName = options?.Flow?.TopologyName ?? ruleConfig?.TopologyName;

            if (!string.IsNullOrEmpty(ruleFlowTopologyName))
            {
                // Execute graph
                // 1. Load graph
                var graph = await LoadGraph(ruleFlowTopologyName, agent, trigger, options?.Flow);
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
    private async Task<RuleGraph?> LoadGraph(string name, Agent agent, IRuleTrigger trigger, RuleFlowOptions? options)
    {
        var flow = _services.GetServices<IRuleFlow<RuleGraph>>().FirstOrDefault(x => x.Name.IsEqualTo(name));
        if (flow == null)
        {
            return null;
        }

        try
        {
            var config = await flow.GetTopologyConfigAsync(options: new()
            {
                TopologyName = name
            });

            var topologyId = config?.TopologyId;
            if (string.IsNullOrEmpty(topologyId))
            {
                return null;
            }


            var param = new Dictionary<string, object>(options?.Parameters ?? []);
            param["agent"] = param.GetValueOrDefault("agent", agent.Name);
            param["agent_id"] = param.GetValueOrDefault("agent_id", agent.Id);
            param["trigger"] = param.GetValueOrDefault("trigger", trigger.Name);

            var graph = await flow.GetTopologyAsync(topologyId, options: new()
            {
                Query = options?.Query,
                Parameters = param
            });

            if (graph != null)
            {
                // Apply input/output schemas from node config to the node
                LoadConfigSchemas(graph);

                // Validate input/output schema compatibility between connected nodes
                if (options?.SkipValidation != true)
                {
                    ValidateGraphSchema(graph);
                }
            }

            return graph;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when loading graph (name: {name}, agent: {agent}, trigger: {trigger?.Name})");
            return null;
        }
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
        try
        {
            await ExecuteGraphTraversal(node, graph, agent, trigger, text, states, data, options, results);
        }
        catch { }
    }

    /// <summary>
    /// Unified graph traversal that uses a swappable frontier.
    /// Stack frontier → DFS, Queue frontier → BFS.
    /// A node or edge can request a mid-traversal switch via its
    /// <c>Config["traversal_algorithm"]</c> value ("dfs" or "bfs").
    /// </summary>
    private async Task ExecuteGraphTraversal(
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
        var flow = options?.Flow;
        var maxRecursion = flow?.MaxRecursion > 0 ? flow.MaxRecursion : RuleConstant.MAX_GRAPH_RECURSION;
        var innerData = new Dictionary<string, string?>(data ?? []);

        // Choose initial frontier based on the global option
        var useBfs = options?.Flow?.TraversalAlgorithm?.IsEqualTo("bfs") == true;
        IFrontier<(RuleNode Node, RuleEdge Edge)> frontier = useBfs
            ? new QueueFrontier<(RuleNode, RuleEdge)>()
            : new StackFrontier<(RuleNode, RuleEdge)>();

        // Seed the frontier with root's children
        foreach (var child in graph.GetChildrenNodes(root))
        {
            frontier.Add(child);
        }

        while (frontier.Count > 0)
        {
            if (results.Count >= maxRecursion)
            {
                _logger.LogWarning("Exceed max graph nodes {MaxNodes} (agent {Agent} and trigger {Trigger}).",
                    maxRecursion, agent.Name, trigger.Name);
                break;
            }

            var (nextNode, nextEdge) = frontier.Remove();

            // Check whether node requests a traversal switch
            frontier = SwitchFrontier(frontier, nextNode);

            // Build context
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

            if (RuleConstant.CONDITION_NODE_TYPES.Contains(nextNode.Type, StringComparer.OrdinalIgnoreCase))
            {
                var conditionResult = await ExecuteCondition(nextNode, nextEdge, graph, agent, trigger, context);
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

                if (conditionResult.Success)
                {
                    EnqueueChildren(frontier, graph, nextNode);
                }
                else
                {
                    _logger.LogInformation("Condition {ConditionName} evaluated to false, skipping next node (agent {Agent} and trigger {Trigger}).",
                        nextNode.Name, agent.Name, trigger.Name);
                }
            }
            else if (RuleConstant.ACTION_NODE_TYPES.Contains(nextNode.Type, StringComparer.OrdinalIgnoreCase)
                  || RuleConstant.ROOT_NODE_TYPES.Contains(nextNode.Type, StringComparer.OrdinalIgnoreCase)
                  || RuleConstant.END_NODE_TYPES.Contains(nextNode.Type, StringComparer.OrdinalIgnoreCase))
            {
                var actionResult = await ExecuteAction(nextNode, nextEdge, graph, agent, trigger, context);
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
                    EnqueueChildren(frontier, graph, nextNode);
                }
            }
            else
            {
                results.Add(RuleFlowStepResult.FromResult(new()
                {
                    Success = true,
                    Response = $"Pass through node {nextNode.Name}."
                }, nextNode));

                EnqueueChildren(frontier, graph, nextNode);
            }
        }
    }

    /// <summary>
    /// If the node carries a <c>traversal_algorithm</c> config value
    /// that differs from the current frontier type, swap to the requested one
    /// and drain all pending items into the new frontier.
    /// </summary>
    private static IFrontier<(RuleNode, RuleEdge)> SwitchFrontier(
        IFrontier<(RuleNode, RuleEdge)> current,
        RuleNode? node)
    {
        // Edge config takes precedence over node config
        var hint = node?.Config?.GetValueOrDefault("traversal_algorithm");

        if (string.IsNullOrEmpty(hint))
        {
            return current;
        }

        var requireBfs = hint.Equals("bfs", StringComparison.OrdinalIgnoreCase);
        var currentBfs = current is QueueFrontier<(RuleNode, RuleEdge)>;

        if (requireBfs == currentBfs)
        {
            return current;
        }

        IFrontier<(RuleNode, RuleEdge)> next = requireBfs
            ? new QueueFrontier<(RuleNode, RuleEdge)>()
            : new StackFrontier<(RuleNode, RuleEdge)>();

        current.DrainTo(next);
        return next;
    }

    private static void EnqueueChildren(
        IFrontier<(RuleNode Node, RuleEdge Edge)> frontier,
        RuleGraph graph,
        RuleNode parent)
    {
        foreach (var child in graph.GetChildrenNodes(parent))
        {
            frontier.Add(child);
        }
    }
    #endregion


    #region Schema Validation
    /// <summary>
    /// Reads "input_schema" and "output_schema" from each node's Config,
    /// deserializes them into FlowUnitSchema, and sets them on the RuleNode.
    /// If a node has no config schema, the code-defined schema from the
    /// resolved IRuleFlowUnit is used as fallback during validation.
    /// </summary>
    private void LoadConfigSchemas(RuleGraph graph)
    {
        var nodes = graph.GetNodes();
        if (nodes == null)
        {
            return;
        }

        foreach (var node in nodes)
        {
            if (node.Config.IsNullOrEmpty())
            {
                continue;
            }

            if (node.Config!.TryGetValue(RuleConstant.INPUT_SCHEMA_KEY, out var inputJson)
                && !string.IsNullOrEmpty(inputJson))
            {
                try
                {
                    node.InputSchema = JsonSerializer.Deserialize<FlowUnitSchema>(inputJson);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize input_schema from config of node [{NodeName}].", node.Name);
                }
            }

            if (node.Config!.TryGetValue(RuleConstant.OUTPUT_SCHEMA_KEY, out var outputJson)
                && !string.IsNullOrEmpty(outputJson))
            {
                try
                {
                    node.OutputSchema = JsonSerializer.Deserialize<FlowUnitSchema>(outputJson);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize output_schema from config of node [{NodeName}].", node.Name);
                }
            }
        }
    }

    /// <summary>
    /// Validates that for every edge in the graph, the downstream node's required input fields
    /// can be satisfied by the upstream node's output or the downstream node's own config.
    /// Node-level schemas (from config) take precedence over code-defined schemas.
    /// </summary>
    private void ValidateGraphSchema(RuleGraph graph)
    {
        var edges = graph.GetEdges();
        if (edges == null || !edges.Any())
        {
            return;
        }

        foreach (var edge in edges)
        {
            if (edge.From == null || edge.To == null)
            {
                continue;
            }

            var sourceUnit = ResolveFlowUnit(edge.From);
            var targetUnit = ResolveFlowUnit(edge.To);

            // Config-defined schema on the node takes precedence over code-defined
            var targetInputSchema = edge.To.InputSchema ?? targetUnit?.InputSchema;
            if (targetInputSchema?.Required == null || targetInputSchema.Required.Count == 0)
            {
                continue;
            }

            // Collect available keys from upstream output and downstream node's own config
            var availableKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var sourceOutputSchema = edge.From.OutputSchema ?? sourceUnit?.OutputSchema;
            if (sourceOutputSchema?.Properties != null && !sourceOutputSchema.Properties.Keys.IsNullOrEmpty())
            {
                foreach (var key in sourceOutputSchema.Properties.Keys)
                {
                    availableKeys.Add(key);
                }
            }

            if (edge.To.Config != null && !edge.To.Config.Keys.IsNullOrEmpty())
            {
                foreach (var key in edge.To.Config.Keys)
                {
                    availableKeys.Add(key);
                }
            }

            // Check each required input field
            foreach (var key in targetInputSchema.Required)
            {
                if (!availableKeys.Contains(key))
                {
                    _logger.Log(
#if DEBUG
                        LogLevel.Critical,
#else
                        LogLevel.Warning,
#endif
                        "Schema validation: edge [{SourceNode}] -> [{TargetNode}]: " +
                        "required input '{Key}' is not provided by upstream output or node config.",
                        edge.From.Name, edge.To.Name, key);
                }
                // Validate type compatibility when both schemas define the property
                else if (sourceOutputSchema?.Properties != null
                    && sourceOutputSchema.Properties.TryGetValue(key, out var sourceProp)
                    && targetInputSchema.Properties.TryGetValue(key, out var targetProp)
                    && !string.IsNullOrEmpty(sourceProp.Type)
                    && !string.IsNullOrEmpty(targetProp.Type)
                    && !sourceProp.Type.Equals(targetProp.Type, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Log(
#if DEBUG
                        LogLevel.Critical,
#else
                        LogLevel.Warning,
#endif
                        "Schema validation: edge [{SourceNode}] -> [{TargetNode}]: " +
                        "type mismatch for '{Key}' — upstream produces '{SourceType}' but downstream expects '{TargetType}'.",
                        edge.From.Name, edge.To.Name, key, sourceProp.Type, targetProp.Type);
                }
            }
        }
    }

    /// <summary>
    /// Resolves the IRuleFlowUnit (action or condition) implementation for a given node.
    /// </summary>
    private IRuleFlowUnit? ResolveFlowUnit(RuleNode node)
    {
        if (node == null || string.IsNullOrEmpty(node.Name))
        {
            return null;
        }

        if (RuleConstant.ROOT_NODE_TYPES.Contains(node.Type, StringComparer.OrdinalIgnoreCase))
        {
            return _services.GetServices<IRuleRoot>()
                            .FirstOrDefault(x => x.Name.IsEqualTo(node.Name));
        }

        if (RuleConstant.END_NODE_TYPES.Contains(node.Type, StringComparer.OrdinalIgnoreCase))
        {
            return _services.GetServices<IRuleEnd>()
                            .FirstOrDefault(x => x.Name.IsEqualTo(node.Name));
        }

        if (RuleConstant.ACTION_NODE_TYPES.Contains(node.Type, StringComparer.OrdinalIgnoreCase))
        {
            return _services.GetServices<IRuleAction>()
                            .FirstOrDefault(x => x.Name.IsEqualTo(node.Name));
        }

        if (RuleConstant.CONDITION_NODE_TYPES.Contains(node.Type, StringComparer.OrdinalIgnoreCase))
        {
            return _services.GetServices<IRuleCondition>()
                            .FirstOrDefault(x => x.Name.IsEqualTo(node.Name));
        }

        return null;
    }
#endregion


    #region Action
    private async Task<RuleNodeResult?> ExecuteAction(
        RuleNode node,
        RuleEdge incomingEdge,
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
                await hook.BeforeRuleActionExecuting(agent, node, incomingEdge, trigger, context);
            }

            // Execute action
            context.Parameters ??= [];
            var result = await foundAction.ExecuteAsync(agent, trigger, context);

            foreach (var hook in hooks)
            {
                await hook.AfterRuleActionExecuted(agent, node, incomingEdge, trigger, context, result);
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
        RuleEdge incomingEdge,
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
                await hook.BeforeRuleConditionExecuting(agent, node, incomingEdge, trigger, context);
            }

            // Execute condition
            context.Parameters ??= [];
            var result = await foundCondition.EvaluateAsync(agent, trigger, context);

            foreach (var hook in hooks)
            {
                await hook.AfterRuleConditionExecuted(agent, node, incomingEdge, trigger, context, result);
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
