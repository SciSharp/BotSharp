using BotSharp.Abstraction.Functions;

namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    public async Task<bool> InvokeFunction(string name, RoleDialogModel message)
    {
        var function = _services.GetServices<IFunctionCallback>().FirstOrDefault(x => x.Name == name);
        if (function == null) return false;

        message.FunctionName = name;
        return await function.Execute(message);
    }
}
