using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Abstraction.Routing;

public interface IRoutingService
{
    List<RoleDialogModel> Dialogs { get; }
    void ResetRecursiveCounter();
    void RefreshDialogs();
    Task<FunctionCallFromLlm> GetNextInstruction();
    Task<bool> InvokeAgent(string agentId, RoleDialogModel message);
    Task<bool> InstructLoop(RoleDialogModel message);
    Task<bool> ExecuteOnce(Agent agent, RoleDialogModel message);
}
