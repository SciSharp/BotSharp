using BotSharp.Core.Rules.Models;

namespace BotSharp.Core.Rules.Conditions;

/// <summary>
/// A gate condition node that collects results from multiple parent condition nodes
/// and evaluates a composite logical expression.
///
/// Supported operators:
///   "and" - All children must evaluate to true (logical conjunction).
///   "or"  - At least one child must evaluate to true (logical disjunction).
///   "not" - Negates a single child (unary operator, only the first child is evaluated).
///
/// Operators can be nested to form arbitrarily complex expressions, e.g.:
///   (A AND B) OR (C AND NOT D)
///
/// Leaf node format:
///   { "node_alias": "node_alias", "key": "data_key" }
///   - "node_alias": The Alias of a parent condition node whose result to inspect.
///                   Using Alias instead of Name avoids collisions when multiple nodes
///                   share the same Name (e.g. several "http_request" nodes).
///   - "key":   The key in the parent node's RuleNodeResult.Data dictionary that holds
///              a boolean string ("true"/"false"). If omitted, falls back to the parent
///              node's RuleNodeResult.Success flag.
///
/// Node config:
///   "expression" - A JSON-encoded LogicExpression tree.
///   "default_value" - The default boolean value ("true"/"false") when a referenced
///                     parent node or data key is not found. Defaults to "false".
///
/// Example: work_order_valid AND (client_name_valid OR NOT affiliate_name_valid)
///
///   Given three parent condition nodes:
///     - Node A (node_alias "check_work_order")  returns Data["work_order_valid"] = "true"
///     - Node B (node_alias "check_client")      returns Data["client_name_valid"] = "false"
///     - Node C (node_alias "check_affiliate")   returns Data["affiliate_name_valid"] = "false"
///
///   The gate node config would be:
///     {
///       "expression": {
///         "op": "and",
///         "children": [
///           { "node_alias": "check_work_order", "key": "work_order_valid" },
///           { "op": "or", "children": [
///               { "node_alias": "check_client", "key": "client_name_valid" },
///               { "op": "not", "children": [
///                   { "node_alias": "check_affiliate", "key": "affiliate_name_valid" }
///               ]}
///           ]}
///         ]
///       },
///       "default_value": "false"
///     }
///
///   Evaluation: true AND (false OR NOT false) => true AND (false OR true) => true AND true => true
/// </summary>
public class LogicGateCondition : IRuleCondition
{
    private readonly ILogger<LogicGateCondition> _logger;

    public LogicGateCondition(
        ILogger<LogicGateCondition> logger)
    {
        _logger = logger;
    }

    public string Name => "logic_gate";

    public FlowUnitSchema? InputSchema => new(
        properties: new()
        {
            ["expression"] = new("object", "A JSON-encoded LogicExpression tree"),
            ["default_value"] = new("string", "Default boolean value when a referenced parent node or data key is not found")
        },
        required: ["expression"]
    );

    public FlowUnitSchema? OutputSchema => new();

    public async Task<RuleNodeResult> EvaluateAsync(
        Agent agent,
        IRuleTrigger trigger,
        RuleFlowContext context)
    {
        var currentNode = context.Node;

        // 1. Ensure all parent nodes have been visited
        var parents = context.Graph.GetParentNodes(currentNode);
        var parentNodeIds = parents.Select(x => x.Item1.Id).ToHashSet();
        var visitedNodeIds = context.PrevStepResults?
            .Select(x => x.Node.Id).ToHashSet() ?? [];

        if (!parentNodeIds.All(id => visitedNodeIds.Contains(id)))
        {
            _logger.LogInformation(
                "Logic gate {NodeName}: not all parent nodes visited yet, deferring (agent {AgentId}).",
                currentNode.Name, agent.Id);
            return new RuleNodeResult
            {
                Success = false,
                Response = "Not all parent nodes have been visited yet."
            };
        }

        // 2. Parse the expression from node config
        var expressionJson = currentNode.Config?.GetValueOrDefault("expression");
        if (string.IsNullOrEmpty(expressionJson))
        {
            _logger.LogWarning("Logic gate {NodeName} has no expression configured.", currentNode.Name);
            return new RuleNodeResult
            {
                Success = false,
                ErrorMessage = "No expression configured for logic gate."
            };
        }

        LogicExpression? expression;
        try
        {
            expression = JsonSerializer.Deserialize<LogicExpression>(expressionJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse logic gate expression for node {NodeName}.", currentNode.Name);
            return new RuleNodeResult
            {
                Success = false,
                ErrorMessage = $"Invalid expression JSON: {ex.Message}"
            };
        }

        if (expression == null)
        {
            return new RuleNodeResult
            {
                Success = false,
                ErrorMessage = "Expression deserialized to null."
            };
        }

        var defaultValue = currentNode.Config?.GetValueOrDefault("default_value") ?? "false";

        // 3. Build lookup: parent node alias → its latest RuleFlowStepResult
        var parentResults = (context.PrevStepResults ?? [])
            .Where(r => parentNodeIds.Contains(r.Node.Id))
            .GroupBy(r => r.Node.Alias, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last(), StringComparer.OrdinalIgnoreCase);

        // 4. Evaluate the expression tree
        var result = Evaluate(expression, parentResults, defaultValue);

        _logger.LogInformation(
            "Logic gate {NodeName} evaluated to {Result} (agent {AgentId}).",
            currentNode.Name, result, agent.Id);

        return new RuleNodeResult
        {
            Success = result,
            Response = result ? "Logic gate: all conditions met." : "Logic gate: conditions not met."
        };
    }

    private bool Evaluate(
        LogicExpression expr,
        Dictionary<string, RuleFlowStepResult> parentResults,
        string defaultValue)
    {
        // Leaf node: look up a specific parent's result by alias
        if (!string.IsNullOrEmpty(expr.NodeAlias))
        {
            if (!parentResults.TryGetValue(expr.NodeAlias, out var stepResult))
            {
                _logger.LogWarning("Logic gate: parent node alias '{Alias}' not found in results, using default '{Default}'.",
                    expr.NodeAlias, defaultValue);
                return ParseBool(defaultValue);
            }

            // If no custom key specified, fall back to the node's Success flag
            if (string.IsNullOrEmpty(expr.Key))
            {
                return stepResult.Success;
            }

            var value = stepResult.Data?.GetValueOrDefault(expr.Key, defaultValue) ?? defaultValue;
            return ParseBool(value);
        }

        // Operator node
        var op = expr.Op?.ToLowerInvariant();
        var children = expr.Children ?? [];

        return op switch
        {
            "and" => children.All(c => Evaluate(c, parentResults, defaultValue)),
            "or" => children.Any(c => Evaluate(c, parentResults, defaultValue)),
            "not" when children.Count > 0 => !Evaluate(children[0], parentResults, defaultValue),
            _ => throw new InvalidOperationException($"Unknown or invalid logic gate operator: '{expr.Op}'")
        };
    }

    private static bool ParseBool(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }
        return bool.TryParse(value, out var b) && b;
    }
}
