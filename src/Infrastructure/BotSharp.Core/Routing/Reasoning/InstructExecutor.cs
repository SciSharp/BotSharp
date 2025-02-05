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

        if (inst.ExecutingDirectly)
        {
            message.Content = inst.Question;
        }

        var msg = RoleDialogModel.From(message, role: AgentRole.Function);
        msg.FunctionArgs = JsonSerializer.Serialize(inst);
        
        await routing.InvokeFunction(message.FunctionName, msg);

        // For client display purpose
        var response = dialogs.Last();
        response.MessageId = message.MessageId;
        response.Instruction = inst;

        return response;
    }
}
