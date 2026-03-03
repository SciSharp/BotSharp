using BotSharp.Abstraction.Templating;
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
                var criteriaResult = await ExecuteCriteriaAsync(agent, rule.RuleCriteria, trigger, text, states, options);
                if (criteriaResult?.IsValid == false)
                {
                    _logger.LogWarning("Criteria validation failed for agent {AgentId} with trigger {TriggerName}", agent.Id, trigger.Name);
                    continue;
                }
            }

            // Execute actions
            // 1. Load graph (agent id, rule name)
            var graph = await LoadGraph(agent.Id, trigger);
            if (graph == null)
            {
                continue;
            }

            // 2. Get root node
            var root = graph.GetRootNode();
            if (root == null)
            {
                continue;
            }

            // 3. Execute graph
            var execResults = new List<RuleActionStepResult>();
            await ExecuteGraphNode(root, graph, agent, trigger, text, states, options, execResults);

            var convIds = execResults.Where(x => x.Success && x.Data.TryGetValue("conversation_id", out _))
                                     .Select(x => x.Data.GetValueOrDefault("conversation_id", string.Empty))
                                     .Where(x => !string.IsNullOrEmpty(x))
                                     .ToList();

            newConversationIds.AddRange(convIds);
        }

        return newConversationIds;
    }

    public async Task ExecuteGraphNode(RuleNode node, RuleGraph graph, IRuleTrigger trigger, RuleNodeExecutionOptions options)
    {
        if (node == null || graph == null || options == null)
        {
            return;
        }

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.GetAgent(options.AgentId);

        var triggerOptions = new RuleTriggerOptions
        {
            MaxGraphRecursion = options.MaxGraphRecursion
        };

        var execResults = new List<RuleActionStepResult>();
        await ExecuteGraphNode(
            node, graph,
            agent, trigger,
            options.Text,
            options.States,
            triggerOptions,
            execResults);
    }

    private async Task<RuleGraph> LoadGraph(string agentId, IRuleTrigger trigger, RuleGraphOptions? options = null)
    {
        var graph = RuleGraph.Init();
        var root = new RuleNode
        {
            Name = "root",
            Type = "root",
        };

        var delayNode = new RuleNode
        {
            Name = "delay_message",
            Type = "action",
            Config = new()
            {
                ["delay"] = "3 seconds"
            }
        };

        var node1 = new RuleNode
        {
            Name = "http_request",
            Type = "action",
            Config = new()
            {
                ["http_method"] = "GET",
                ["http_url"] = "https://dummy.restapiexample.com/api/v1/employees"
            }
        };

        var node2 = new RuleNode
        {
            Name = "http_request",
            Type = "action",
            Config = new()
            {
                ["http_method"] = "GET",
                ["http_url"] = "https://dummy.restapiexample.com/api/v1/employee/1"
            }
        };

        var node3 = new RuleNode
        {
            Name = "http_request",
            Type = "action",
            Config = new()
            {
                ["http_method"] = "GET",
                ["http_url"] = "https://dummy.restapiexample.com/api/v1/employee/2"
            }
        };

        graph.AddEdge(root, delayNode, payload: new()
        {
            Name = "edge",
            Type = "is_next"
        });

        graph.AddEdge(delayNode, node1, payload: new()
        {
            Name = "edge",
            Type = "is_next"
        });

        graph.AddEdge(node1, node2, payload: new()
        {
            Name = "edge",
            Type = "is_next"
        });

        graph.AddEdge(node1, node3, payload: new()
        {
            Name = "edge",
            Type = "is_next"
        });

        return graph;
    }


    private async Task<RuleGraph> LoadDefaultGraph()
    {
        var graph = RuleGraph.Init();
        var root = new RuleNode
        {
            Name = "root",
            Type = "root",
        };

        var node = new RuleNode
        {
            Name = "send_message_to_agent",
            Type = "action"
        };

        graph.AddEdge(root, node, payload: new()
        {
            Name = "edge",
            Type = "is_next"
        });

        return graph;
    }


    private async Task ExecuteGraphNode(
        RuleNode node,
        RuleGraph graph,
        Agent agent,
        IRuleTrigger trigger,
        string text,
        IEnumerable<MessageState>? states,
        RuleTriggerOptions? options,
        List<RuleActionStepResult> results)
    {
        var maxRecursion = options?.MaxGraphRecursion ?? RuleConstant.MAX_GRAPH_RECURSION;
        if (results.Count >= maxRecursion)
        {
            _logger.LogWarning("Exceed max graph recursion {MaxRecursion} (agent {Agent} and trigger {Trigger}).",
                maxRecursion, agent.Name, trigger.Name);
            return;
        }

        var neighbors = graph.GetNeighbors(node);
        foreach (var (neighborNode, edge) in neighbors)
        {
            if (!neighborNode.Type.IsEqualTo("action"))
            {
                continue;
            }

            var actions = _services.GetServices<IRuleAction>();
            var action = actions.FirstOrDefault(x => x.Name.IsEqualTo(neighborNode.Name));
            if (action == null)
            {
                continue;
            }

            var context = new RuleActionContext
            {
                Node = neighborNode,
                Graph = graph,
                Text = text,
                Parameters = BuildContextParameters(neighborNode.Config, states),
                PrevStepResults = results,
                JsonOptions = options?.JsonOptions
            };

            // Check whether the edge is executable from source node to target node
            var isExecutable = await IsExecutable(edge, agent, trigger, context);
            if (!isExecutable)
            {
                continue;
            }

            // Execute action
            var actionResult = await ExecuteAction(neighborNode, graph, agent, trigger, context);
            results.Add(new RuleActionStepResult
            {
                Node = neighborNode,
                Success = actionResult.Success,
                Response = actionResult.Response,
                Data = new(actionResult.Data ?? []),
                ErrorMessage = actionResult.ErrorMessage,
                IsDelayed = actionResult.IsDelayed
            });

            if (results.Count >= maxRecursion)
            {
                _logger.LogWarning("Exceed max graph recursion {MaxRecursion} (agent {Agent} and trigger {Trigger}).",
                                    maxRecursion, agent.Name, trigger.Name);
                break;
            }

            if (actionResult.IsDelayed)
            {
                continue;
            }

            await ExecuteGraphNode(neighborNode, graph, agent, trigger, text, states, options, results);
        }
    }


    private async Task<bool> IsExecutable(RuleEdge edge, Agent agent, IRuleTrigger triger, RuleActionContext context)
    {
        return true;
    }


    #region Criteria
    private async Task<RuleCriteriaResult> ExecuteCriteriaAsync(
        Agent agent,
        AgentRuleCriteria ruleCriteria,
        IRuleTrigger trigger,
        string text,
        IEnumerable<MessageState>? states,
        RuleTriggerOptions? triggerOptions)
    {
        var result = new RuleCriteriaResult();

        try
        {
            var criteria = _services.GetServices<IRuleCriteria>()
                                    .FirstOrDefault(x => x.Provider == ruleCriteria.Name);

            if (criteria == null)
            {
                return result;
            }


            var context = new RuleCriteriaContext
            {
                Text = text,
                Parameters = BuildContextParameters(ruleCriteria.Config, states),
                JsonOptions = triggerOptions?.JsonOptions
            };

            _logger.LogInformation("Start execution rule criteria {CriteriaProvider} for agent {AgentId} with trigger {TriggerName}",
                criteria.Provider, agent.Id, trigger.Name);

            var hooks = _services.GetHooks<IRuleTriggerHook>(agent.Id);
            foreach (var hook in hooks)
            {
                await hook.BeforeRuleCriteriaExecuted(agent, ruleCriteria, trigger, context);
            }

            // Execute criteria
            context.Parameters ??= [];
            result = await criteria.ValidateAsync(agent, trigger, context);

            foreach (var hook in hooks)
            {
                await hook.AfterRuleCriteriaExecuted(agent, ruleCriteria, trigger, result);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing rule criteria {CriteriaProvider} for agent {AgentId}", ruleCriteria.Name, agent.Id);
            return result;
        }
    }
    #endregion


    #region Action
    private async Task<RuleActionResult> ExecuteAction(
        RuleNode node,
        RuleGraph graph,
        Agent agent,
        IRuleTrigger trigger,
        RuleActionContext context)
    {
        try
        {
            // Get all registered rule actions
            var actions = _services.GetServices<IRuleAction>();

            // Find the matching action
            var foundAction = actions.FirstOrDefault(x => x.Name.IsEqualTo(node?.Name));

            if (foundAction == null)
            {
                var errorMsg = $"No rule action {node?.Name} is found";
                _logger.LogWarning(errorMsg);
                return RuleActionResult.Failed(errorMsg);
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
            return RuleActionResult.Failed(ex.Message);
        }
    }
    #endregion


    #region Private methods
    private Dictionary<string, string?> BuildContextParameters(JsonDocument? config, IEnumerable<MessageState>? states)
    {
        var dict = new Dictionary<string, string?>();

        if (config != null)
        {
            dict = ConvertToDictionary(config);
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

    private static Dictionary<string, string?> ConvertToDictionary(JsonDocument doc)
    {
        var dict = new Dictionary<string, string?>();

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            object? value = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString(),
                JsonValueKind.Number when prop.Value.TryGetDecimal(out decimal decimalValue) => decimalValue,
                JsonValueKind.Number when prop.Value.TryGetDouble(out double doubleValue) => doubleValue,
                JsonValueKind.Number when prop.Value.TryGetInt32(out int intValue) => intValue,
                JsonValueKind.Number when prop.Value.TryGetInt64(out long longValue) => longValue,
                JsonValueKind.Number when prop.Value.TryGetDateTime(out DateTime dateTimeValue) => dateTimeValue,
                JsonValueKind.Number when prop.Value.TryGetDateTimeOffset(out DateTimeOffset dateTimeOffsetValue) => dateTimeOffsetValue,
                JsonValueKind.Number when prop.Value.TryGetGuid(out Guid guidValue) => guidValue,
                JsonValueKind.Number => prop.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Undefined => null,
                JsonValueKind.Array => prop.Value,
                JsonValueKind.Object => prop.Value,
                _ => prop.Value
            };
            dict[prop.Name] = value?.ConvertToString();
        }

        return dict;
        #endregion
    }

    private int? RenderSkippingExpression(string? expression, Dictionary<string, string?> dict)
    {
        int? steps = null;

        if (string.IsNullOrWhiteSpace(expression))
        {
            return steps;
        }

        var render = _services.GetRequiredService<ITemplateRender>();
        var copy = dict != null
            ? new Dictionary<string, object>(dict.Where(x => x.Value != null).ToDictionary(x => x.Key, x => (object)x.Value!))
            : [];
        var result = render.Render(expression, new Dictionary<string, object>
        {
            { "states", copy }
        });

        if (int.TryParse(result, out var intVal))
        {
            steps = intVal;
        }
        else if (bool.TryParse(result, out var boolVal) && boolVal)
        {
            steps = 1;
        }

        return steps;
    }
}
