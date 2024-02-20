using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Abstraction.Routing.Planning;

/// <summary>
/// Task breakdown and execution plan 
/// https://www.promptingguide.ai/techniques/cot
/// </summary>
public interface IPlaner
{
    Task<FunctionCallFromLlm> GetNextInstruction(Agent router, string messageId, List<RoleDialogModel> dialogs);
    Task<bool> AgentExecuting(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs);
    Task<bool> AgentExecuted(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs);
    List<RoleDialogModel> BeforeHandleContext(FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
        => dialogs;
    bool AfterHandleContext(List<RoleDialogModel> dialogs, List<RoleDialogModel> taskAgentDialogs)
        => true;
    int MaxLoopCount => 5;
}
