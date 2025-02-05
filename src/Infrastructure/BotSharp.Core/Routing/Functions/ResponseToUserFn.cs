using BotSharp.Abstraction.Functions;

namespace BotSharp.Core.Routing.Functions;

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

    public async Task<bool> Execute(RoleDialogModel message)
    {
        return true;
    }
}
