using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Abstraction.Routing.Planning;

public interface IExecutor
{
    Task<RoleDialogModel> Execute(IRoutingService routing,
        FunctionCallFromLlm inst,
        RoleDialogModel message,
        List<RoleDialogModel> dialogs);
}
