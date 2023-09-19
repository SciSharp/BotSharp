namespace BotSharp.Abstraction.Routing;

public interface IRoutingService
{
    List<RoleDialogModel> Dialogs { get; }
    Task<RoleDialogModel> Enter(Agent agent, List<RoleDialogModel> whileDialogs);
}
