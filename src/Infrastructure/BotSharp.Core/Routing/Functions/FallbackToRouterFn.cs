using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.Core.Routing.Functions;

public class FallbackToRouterFn : IFunctionCallback
{
    public string Name => "util-routing-fallback_to_router";
    private readonly IServiceProvider _services;

    public FallbackToRouterFn(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<FallbackArgs>(message.FunctionArgs);
        var routing = _services.GetRequiredService<IRoutingService>();
        routing.Context.PopTo(routing.Context.EntryAgentId, "pop to entry agent");
        message.Content = args.Question;

        return true;
    }
}
