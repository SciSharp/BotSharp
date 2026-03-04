namespace BotSharp.Abstraction.Rules.Models;

public class RuleFlowStepResult : RuleNodeResult
{
    public RuleNode Node { get; set; }

    /// <summary>
    /// Create a RuleFlowStepResult from a RuleNodeResult and a RuleNode
    /// </summary>
    public static RuleFlowStepResult FromResult(RuleNodeResult result, RuleNode node)
    {
        return new RuleFlowStepResult
        {
            Node = node,
            Success = result.Success,
            IsValid = result.IsValid,
            Response = result.Response,
            ErrorMessage = result.ErrorMessage,
            Data = new(result.Data ?? []),
            IsDelayed = result.IsDelayed
        };
    }
}
