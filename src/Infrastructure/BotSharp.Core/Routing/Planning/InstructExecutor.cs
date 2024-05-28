using BotSharp.Abstraction.Routing.Planning;

namespace BotSharp.Core.Routing.Planning;

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
        List<RoleDialogModel> dialogs, 
        Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        message.Instruction = inst;

        var handlers = _services.GetServices<IRoutingHandler>();
        var handler = handlers.FirstOrDefault(x => x.Name == inst.Function);
        handler.SetDialogs(dialogs);

        var handled = await handler.Handle(routing, inst, message, onFunctionExecuting);

        // For client display purpose
        var response = dialogs.Last();
        response.MessageId = message.MessageId;
        response.Instruction = inst;

        return response;
    }
}
