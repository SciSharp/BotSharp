using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.Core.Routing.Functions;

/// <summary>
/// Response to user if router doesn't need to route to agent.
/// </summary>
public class ResponseToUserFn : IFunctionCallback
{
    public string Name => "response_to_user";
    private readonly IServiceProvider _services;
    private readonly IRoutingContext _context;

    public ResponseToUserFn(IServiceProvider services, IRoutingContext context)
    {
        _services = services;
        _context = context;
    }

    public Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<ResponseUserArgs>(message.FunctionArgs);
        message.Content = args.Content;
        message.Handled = true;
        message.StopCompletion = true;
        return Task.FromResult(true);
    }
}
