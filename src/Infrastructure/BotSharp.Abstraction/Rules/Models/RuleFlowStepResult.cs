using BotSharp.Abstraction.Graph;

namespace BotSharp.Abstraction.Rules.Models;

public class RuleFlowStepResult : RuleNodeResult
{
    public FlowNode Node { get; set; }

    /// <summary>
    /// Create a RuleFlowStepResult from a RuleNodeResult and a FlowNode
    /// </summary>
    public static RuleFlowStepResult FromResult(RuleNodeResult result, FlowNode node)
    {
        return new RuleFlowStepResult
        {
            Node = node,
            Success = result.Success,
            Response = result.Response,
            ErrorMessage = result.ErrorMessage,
            Data = new(result.Data ?? []),
            IsDelayed = result.IsDelayed
        };
    }
}
