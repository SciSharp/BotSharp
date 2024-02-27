using BotSharp.Abstraction.Functions;

namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    public async Task<bool> InvokeFunction(string name, RoleDialogModel message)
    {
        var function = _services.GetServices<IFunctionCallback>().FirstOrDefault(x => x.Name == name);
        if (function == null) return false;

        var originalFunctionName = message.FunctionName;
        message.FunctionName = name;
        message.Role = AgentRole.Function;
        var result = await function.Execute(message);

        // restore original function name
        if (!message.StopCompletion)
        {
            message.FunctionName = originalFunctionName;
        }

        return result;
    }
}
