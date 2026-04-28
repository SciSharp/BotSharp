namespace BotSharp.Core.Rules.Root;

/// <summary>
/// Default root/start node for a rule flow graph.
/// Passes trigger states and config downstream as its output.
/// No input schema — this is the entry point of the graph.
/// </summary>
public class StartAction : IRuleRoot, IRuleAction
{
    private readonly ILogger<StartAction> _logger;

    public StartAction(ILogger<StartAction> logger)
    {
        _logger = logger;
    }

    public string Name => "start";

    public FlowUnitSchema? OutputSchema => null;

    public Task<RuleNodeResult> ExecuteAsync(
        Agent agent,
        IRuleTrigger trigger,
        RuleFlowContext context)
    {
        _logger.LogInformation("Start node executed for agent {AgentId} with trigger {TriggerName}.",
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
            Response = "Graph started.",
            Data = data
        });
    }
}
