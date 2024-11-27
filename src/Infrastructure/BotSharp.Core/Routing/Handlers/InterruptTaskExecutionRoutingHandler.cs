using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;
using BotSharp.Core.Routing.Reasoning;

namespace BotSharp.Core.Routing.Handlers;

public class InterruptTaskExecutionRoutingHandler : RoutingHandlerBase//, IRoutingHandler
{
    public string Name => "interrupt_task_execution";

    public string Description => "Can't continue user's request becauase the requirements are not met.";

    public List<ParameterPropertyDef> Parameters => new List<ParameterPropertyDef>
    {
        new ParameterPropertyDef("reason", "the reason why the request is interrupted"),
        new ParameterPropertyDef("answer", "the content response to user")
    };

    public List<string> Planers => new List<string>
    {
        nameof(HFReasoner)
    };

    public InterruptTaskExecutionRoutingHandler(IServiceProvider services, ILogger<InterruptTaskExecutionRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public async Task<bool> Handle(IRoutingService routing, FunctionCallFromLlm inst, RoleDialogModel message)
    {
        message.FunctionName = inst.Function;
        message.StopCompletion = true;

        return true;
    }
}
