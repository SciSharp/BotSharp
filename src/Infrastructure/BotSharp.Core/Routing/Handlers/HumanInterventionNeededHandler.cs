using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Handlers;

public class HumanInterventionNeededHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "human_intervention_needed";

    public string Description => "Reach out to human being, customer service or customer representative.";

    public List<NameDesc> Parameters => new List<NameDesc>
    {
        new NameDesc("reason", "why need customer service"),
        new NameDesc("response", "response content to user")
    };

    public HumanInterventionNeededHandler(IServiceProvider services, ILogger<HumanInterventionNeededHandler> logger, RoutingSettings settings)
        : base(services, logger, settings)
    {
        
    }

    public async Task<bool> Handle(IRoutingService routing, FunctionCallFromLlm inst, RoleDialogModel message)
    {
        message.Role = AgentRole.Assistant;
        message.Content = inst.Response;

        var hooks = _services.GetServices<IConversationHook>()
            .OrderBy(x => x.Priority)
            .ToList();

        foreach (var hook in hooks)
        {
            await hook.OnHumanInterventionNeeded(message);
        }

        return true;
    }
}
