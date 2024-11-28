using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Abstraction.Routing.Reasoning;

/// <summary>
/// Reasoning approaches for large language models (LLMs) help enhance their ability to solve complex problems, 
/// handle tasks requiring logic, and provide accurate and contextually appropriate responses.
/// </summary>
public interface IRoutingReasoner
{
    string Name => "Unnamed Reasoner";
    string Description => "Each of these approaches leverages the capabilities of LLMs to reason more effectively, " +
        "ensuring better performance and more coherent outputs across various types of complex tasks.";

    int MaxLoopCount => 5;

    Task<FunctionCallFromLlm> GetNextInstruction(Agent router, string messageId, List<RoleDialogModel> dialogs);

    Task<bool> AgentExecuting(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
        => Task.FromResult(true);

    Task<bool> AgentExecuted(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
        => Task.FromResult(true);

    List<RoleDialogModel> BeforeHandleContext(FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
        => dialogs;

    bool AfterHandleContext(List<RoleDialogModel> dialogs, List<RoleDialogModel> taskAgentDialogs)
        => true;
}
