using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Planning;
using BotSharp.Abstraction.Routing;

namespace BotSharp.Core.Planning;

public class InstructExecutor : IExecutor
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public InstructExecutor(IServiceProvider services, ILogger<InstructExecutor> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(IRoutingService routing,
        Agent router,
        FunctionCallFromLlm inst,
        List<RoleDialogModel> dialogs,
        RoleDialogModel message)
    {
        // Set user content as Planner's question
        inst.Question = message.Content;
        message.Instruction = inst;

        var handlers = _services.GetServices<IRoutingHandler>();

        var handler = handlers.FirstOrDefault(x => x.Name == inst.Function);
        handler.SetRouter(router);
        handler.SetDialogs(dialogs);

        message.FunctionName = inst.Function;
        message.Role = AgentRole.Function;
        message.FunctionArgs = inst.Arguments == null ? "{}" : JsonSerializer.Serialize(inst.Arguments);

        var handled = await handler.Handle(routing, inst, message);

        inst.Response = message.Content;

        return handled;
    }
}
