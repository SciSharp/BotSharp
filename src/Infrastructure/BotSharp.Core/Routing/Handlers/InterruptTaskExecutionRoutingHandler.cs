using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Handlers;

public class InterruptTaskExecutionRoutingHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "interrupt_task_execution";

    public string Description => "Can't continue user's request becauase the requirements are not met.";

    public List<NameDesc> Parameters => new List<NameDesc>
    {
        new NameDesc("reason", "the reason why the request is interrupted"),
        new NameDesc("answer", "the content response to user")
    };

    public bool IsReasoning => true;

    public InterruptTaskExecutionRoutingHandler(IServiceProvider services, ILogger<InterruptTaskExecutionRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public async Task<RoleDialogModel> Handle(FunctionCallFromLlm inst)
    {
        var result = new RoleDialogModel(AgentRole.User, inst.Reason)
        {
            FunctionName = inst.Function,
            StopCompletion = true
        };

        return result;
    }
}
