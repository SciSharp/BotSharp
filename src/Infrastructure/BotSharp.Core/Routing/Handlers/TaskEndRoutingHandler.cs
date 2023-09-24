using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Handlers;

public class TaskEndRoutingHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "task_end";

    public string Description => "Call this function when current task is completed.";

    public List<NameDesc> Parameters => new List<NameDesc>
    {
        new NameDesc("abandoned_arguments", "the arguments next task can't reuse")
    };

    public bool IsReasoning => true;

    public TaskEndRoutingHandler(IServiceProvider services, ILogger<TaskEndRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public Task<RoleDialogModel> Handle(FunctionCallFromLlm inst)
    {
        throw new NotImplementedException();
    }
}
