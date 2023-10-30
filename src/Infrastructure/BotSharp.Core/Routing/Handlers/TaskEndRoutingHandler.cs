using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;
using BotSharp.Core.Planning;

namespace BotSharp.Core.Routing.Handlers;

public class TaskEndRoutingHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "task_end";

    public string Description => "Call this function when current task is completed.";

    public List<ParameterPropertyDef> Parameters => new List<ParameterPropertyDef>
    {
        new ParameterPropertyDef("abandoned_arguments", "the arguments next task can't reuse")
    };

    public List<string> Planers => new List<string>
    {
        nameof(HFPlanner)
    };

    public TaskEndRoutingHandler(IServiceProvider services, ILogger<TaskEndRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public async Task<bool> Handle(IRoutingService routing, FunctionCallFromLlm inst, RoleDialogModel message)
    {
        var hooks = _services.GetServices<IConversationHook>()
            .OrderBy(x => x.Priority)
            .ToList();

        Task.WaitAll(hooks
            .Select(h => h.OnCurrentTaskEnding(message))
            .ToArray());

        return true;
    }
}
