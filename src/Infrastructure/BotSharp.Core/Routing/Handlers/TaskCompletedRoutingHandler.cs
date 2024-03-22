using BotSharp.Abstraction.Routing.Settings;
using BotSharp.Core.Routing.Planning;

namespace BotSharp.Core.Routing.Handlers;

public class TaskCompletedRoutingHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "task_completed";

    public string Description => "User task is completed.";

    public List<ParameterPropertyDef> Parameters => new List<ParameterPropertyDef>
    {
        new ParameterPropertyDef("reason", "why the task is completed")
        {
            Required = true
        },
        new ParameterPropertyDef("response", "polite response when the task is completed")
        {
            Required = true
        },
        new ParameterPropertyDef("conversation_end", "whether to end this conversation, true or false")
        {
            Required = true,
            Type = "boolean"
        },
        new ParameterPropertyDef("abandoned_arguments", "the arguments next task can't reuse")
    };

    public List<string> Planers => new List<string>
    {
        nameof(NaivePlanner),
        nameof(HFPlanner)
    };

    public TaskCompletedRoutingHandler(IServiceProvider services, ILogger<TaskCompletedRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public async Task<bool> Handle(IRoutingService routing, FunctionCallFromLlm inst, RoleDialogModel message)
    {
        var response = new RoleDialogModel(AgentRole.Assistant, inst.Response)
        {
            CurrentAgentId = message.CurrentAgentId,
            MessageId = message.MessageId,
            StopCompletion = true,
            FunctionName = inst.Function,
            Instruction = inst,
        };

        _dialogs.Add(response);

        var hooks = _services.GetServices<IConversationHook>()
            .OrderBy(x => x.Priority)
            .ToList();

        foreach (var hook in hooks)
        {
            await hook.OnTaskCompleted(response);
        }

        return true;
    }
}
