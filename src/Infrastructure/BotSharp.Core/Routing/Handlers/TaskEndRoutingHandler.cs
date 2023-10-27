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

    public async Task<RoleDialogModel> Handle(IRoutingService routing, FunctionCallFromLlm inst)
    {
        var result = new RoleDialogModel(AgentRole.Assistant, inst.Response)
        {
            MessageId = inst.MessageId,
            CurrentAgentId = _settings.RouterId,
            FunctionName = inst.Function,
            Data = inst
        };

        var hooks = _services.GetServices<IConversationHook>()
            .OrderBy(x => x.Priority)
            .ToList();

        foreach (var hook in hooks)
        {
            await hook.OnCurrentTaskEnding(result);
        }

        return result;
    }
}
