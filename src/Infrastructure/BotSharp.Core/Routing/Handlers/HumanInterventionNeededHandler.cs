using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Handlers;

public class HumanInterventionNeededHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "human_intervention_needed";

    public string Description => "Reach out to a real human or customer representative.";

    private readonly RoutingSettings _settings;

    public HumanInterventionNeededHandler(IServiceProvider services, ILogger<HumanInterventionNeededHandler> logger, RoutingSettings settings)
        : base(services, logger, settings)
    {
        _settings = settings;
    }

    public async Task<RoleDialogModel> Handle(IRoutingService routing, FunctionCallFromLlm inst)
    {
        var result = new RoleDialogModel(AgentRole.Assistant, inst.Response)
        {
            CurrentAgentId = _settings.RouterId,
            FunctionName = inst.Function,
            ExecutionData = inst
        };

        var hooks = _services.GetServices<IConversationHook>()
            .OrderBy(x => x.Priority)
            .ToList();

        foreach (var hook in hooks)
        {
            await hook.HumanInterventionNeeded(result);
        }

        return result;
    }
}
