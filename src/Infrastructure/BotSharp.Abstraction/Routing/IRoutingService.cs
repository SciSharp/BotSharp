namespace BotSharp.Abstraction.Routing;

public interface IRoutingService
{
    Agent LoadRouter();
    List<RoleDialogModel> Dialogs { get; }
    void SetDialogs(List<RoleDialogModel> dialogs);
    Task<RoleDialogModel> InstructLoop(Agent router);
    Task<RoleDialogModel> ExecuteOnce(Agent agent);
}
