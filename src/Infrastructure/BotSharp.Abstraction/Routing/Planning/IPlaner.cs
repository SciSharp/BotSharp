using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.Abstraction.Routing.Planning;

/// <summary>
/// Task breakdown and execution plan 
/// https://www.promptingguide.ai/techniques/cot
/// </summary>
public interface IPlaner
{
    Task<FunctionCallFromLlm> GetNextInstruction(Agent router, string messageId, List<RoleDialogModel> dialogs);
    Task<bool> AgentExecuting(Agent router, FunctionCallFromLlm inst, RoleDialogModel message);
    Task<bool> AgentExecuted(Agent router, FunctionCallFromLlm inst, RoleDialogModel message);
    int MaxLoopCount => 5;
}
