namespace BotSharp.Core.Rules.Conditions;

public class AllVisitedRuleCondition : IRuleCondition
{
    private readonly ILogger<AllVisitedRuleCondition> _logger;

    public AllVisitedRuleCondition(
        ILogger<AllVisitedRuleCondition> logger)
    {
        _logger = logger;
    }

    public string Name => "all_visited";

    public FlowUnitSchema? InputSchema => new();

    public FlowUnitSchema? OutputSchema => new();

    public async Task<RuleNodeResult> EvaluateAsync(
        Agent agent,
        IRuleTrigger trigger,
        RuleFlowContext context)
    {
        var currentNode = context.Node;
        var parents = context.Graph.GetParentNodes(currentNode);
        var parentNodeIds = parents.Select(x => x.Item1.Id).ToList();
        var visitedNodeIds = context.PrevStepResults?.Select(x => x.Node.Id)?.ToHashSet() ?? [];
        var allVisited = parentNodeIds.All(x => visitedNodeIds.Contains(x));

        return new RuleNodeResult
        {
            Success = allVisited,
            Response = allVisited ? "All parent nodes have been visited" : "Missing parenet nodes visiting."
        };
    }
}
