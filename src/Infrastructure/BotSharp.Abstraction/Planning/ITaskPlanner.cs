using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Abstraction.Planning;

/// <summary>
/// Planning process for Task Agent
/// https://www.promptingguide.ai/techniques/cot
/// </summary>
public interface ITaskPlanner
{
    string Name => "Unamed Task Planner";
    Task<FunctionCallFromLlm> GetNextInstruction(Agent router, string messageId, List<RoleDialogModel> dialogs);
    Task<bool> AgentExecuting(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs);
    Task<bool> AgentExecuted(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs);
    List<RoleDialogModel> BeforeHandleContext(FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
        => dialogs;
    bool AfterHandleContext(List<RoleDialogModel> dialogs, List<RoleDialogModel> taskAgentDialogs)
        => true;
    int MaxLoopCount => 5;
}
