using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Handlers;

public class HumanInterventionNeededHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "human_intervention_needed";

    public string Description => "Reach out to human customer service.";

    public List<ParameterPropertyDef> Parameters => new List<ParameterPropertyDef>
    {
        new ParameterPropertyDef("reason", "why need customer service"),
        new ParameterPropertyDef("summary", "the whole conversation summary with important information"),
        new ParameterPropertyDef("response", "asking user whether to connect with customer service representative")
    };

    public HumanInterventionNeededHandler(IServiceProvider services, ILogger<HumanInterventionNeededHandler> logger, RoutingSettings settings)
        : base(services, logger, settings)
    {
        
    }

    public async Task<bool> Handle(IRoutingService routing, FunctionCallFromLlm inst, RoleDialogModel message)
    {
        var response = RoleDialogModel.From(message, 
            role: AgentRole.Assistant, 
            content: inst.Response);

        _dialogs.Add(response);

        var hooks = _services.GetServices<IConversationHook>()
            .OrderBy(x => x.Priority)
            .ToList();

        foreach (var hook in hooks)
        {
            await hook.OnHumanInterventionNeeded(response);
        }

        return true;
    }
}
