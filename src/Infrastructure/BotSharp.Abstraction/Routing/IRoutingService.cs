namespace BotSharp.Abstraction.Routing;

public interface IRoutingService
{
    Agent Router { get; }
    void ResetRecursiveCounter();
    Task<bool> InvokeAgent(string agentId, List<RoleDialogModel> dialogs);
    Task<RoleDialogModel> InstructLoop(RoleDialogModel message);
    Task<RoleDialogModel> ExecuteOnce(Agent agent, RoleDialogModel message);
}
