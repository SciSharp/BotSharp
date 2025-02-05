using BotSharp.Abstraction.Functions;

namespace BotSharp.Core.Routing.Functions;

public class ConversationEndFn : IFunctionCallback
{
    public string Name => "conversation_end";
    private readonly IServiceProvider _services;
    private readonly IRoutingContext _context;

    public ConversationEndFn(IServiceProvider services, IRoutingContext context)
    {
        _services = services;
        _context = context;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var inst = JsonSerializer.Deserialize<FunctionCallFromLlm>(message.FunctionArgs);

        message.Content = inst.Response;

        return true;
    }
}
