using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing;

namespace BotSharp.Abstraction.Planning;

public interface IExecutor
{
    Task<bool> Execute(IRoutingService routing, 
        Agent router,
        FunctionCallFromLlm inst,
        List<RoleDialogModel> dialogs,
        RoleDialogModel message);
}
