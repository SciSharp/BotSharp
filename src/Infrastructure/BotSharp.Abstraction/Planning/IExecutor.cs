using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing;

namespace BotSharp.Abstraction.Planning;

public interface IExecutor
{
    Task<RoleDialogModel> Execute(IRoutingService routing,
        FunctionCallFromLlm inst,
        RoleDialogModel message,
        List<RoleDialogModel> dialogs);
}
