using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.Abstraction.Routing.Planning;

/// <summary>
/// Task breakdown and execution plan 
/// https://www.promptingguide.ai/techniques/cot
/// </summary>
public interface IPlaner
{
    Task<FunctionCallFromLlm> GetNextInstruction(Agent router, string messageId);
    Task<bool> AgentExecuting(Agent router, FunctionCallFromLlm inst, RoleDialogModel message);
    Task<bool> AgentExecuted(Agent router, FunctionCallFromLlm inst, RoleDialogModel message);
    bool HideDialogContext => false;
    Task<DecomposedStep> GetDecomposedStepAsync(Agent router, string messageId, List<RoleDialogModel> dialogs)
        => throw new NotImplementedException("");
    int MaxLoopCount => 5;
}
