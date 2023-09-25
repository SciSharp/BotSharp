namespace BotSharp.Abstraction.Routing;

public interface IRoutingService
{
    Agent LoadRouter();
    List<RoleDialogModel> Dialogs { get; }
    Task<RoleDialogModel> InstructLoop();
    Task<RoleDialogModel> ExecuteOnce(Agent agent);
}
