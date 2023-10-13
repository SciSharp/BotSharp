using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Abstraction.Routing;

public interface IRoutingService
{
    List<RoleDialogModel> Dialogs { get; }
    Task<FunctionCallFromLlm> GetNextInstruction();
    Task<RoleDialogModel> InvokeAgent(string agentId);
    Task<RoleDialogModel> InstructLoop();
    Task<RoleDialogModel> ExecuteOnce(Agent agent);
}
