using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Handlers;

public class HumanInterventionNeededHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "human_intervention_needed";

    public string Description => "Reach out to human being, customer service or customer representative.";

    private readonly RoutingSettings _settings;

    public List<NameDesc> Parameters => new List<NameDesc>
    {
        new NameDesc("reason", "why need customer service"),
        new NameDesc("response", "response content to user")
    };

    public HumanInterventionNeededHandler(IServiceProvider services, ILogger<HumanInterventionNeededHandler> logger, RoutingSettings settings)
        : base(services, logger, settings)
    {
        _settings = settings;
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
            await hook.OnHumanInterventionNeeded(result);
        }

        return result;
    }
}
