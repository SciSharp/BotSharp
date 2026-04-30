namespace BotSharp.Core.Rules.Root;

/// <summary>
/// Default end/terminal node for a rule flow graph.
/// Collects final results. No output schema — this is the exit point.
/// </summary>
public class EndAction : IRuleEnd, IRuleAction
{
    private readonly ILogger<EndAction> _logger;

    public EndAction(ILogger<EndAction> logger)
    {
        _logger = logger;
    }

    public string Name => "end";

    public FlowUnitSchema? InputSchema => null;

    public Task<RuleNodeResult> ExecuteAsync(
        Agent agent,
        IRuleTrigger trigger,
        RuleFlowContext context)
    {
        _logger.LogInformation("End node executed for agent {AgentId} with trigger {TriggerName}.",
            agent.Id, trigger.Name);

        var data = new Dictionary<string, string>();
        if (!context.Parameters.IsNullOrEmpty())
        {
            foreach (var kvp in context.Parameters!)
            {
                if (kvp.Value != null)
                {
                    data[kvp.Key] = kvp.Value;
                }
            }
        }

        return Task.FromResult(new RuleNodeResult
        {
            Success = true,
            Response = "Graph completed.",
            Data = data
        });
    }
}
