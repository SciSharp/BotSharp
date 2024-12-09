using BotSharp.Abstraction.Planning;

namespace BotSharp.Core.Routing.Reasoning;

public class InstructExecutor : IExecutor
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public InstructExecutor(IServiceProvider services, ILogger<InstructExecutor> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<RoleDialogModel> Execute(IRoutingService routing,
        FunctionCallFromLlm inst,
        RoleDialogModel message,
        List<RoleDialogModel> dialogs)
    {
        message.Instruction = inst;

        var handlers = _services.GetServices<IRoutingHandler>();
        var handler = handlers.FirstOrDefault(x => x.Name == inst.Function);
        handler.SetDialogs(dialogs);

        var handled = await handler.Handle(routing, inst, message);

        // For client display purpose
        var response = dialogs.Last();
        response.MessageId = message.MessageId;
        response.Instruction = inst;

        return response;
    }
}
