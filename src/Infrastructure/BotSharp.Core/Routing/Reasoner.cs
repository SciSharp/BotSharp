using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing;

public class Reasoner : Router
{
    public override string AgentId => _settings.ReasonerId;

    public Reasoner(IServiceProvider services,
        ILogger<Reasoner> logger,
        RoutingSettings settings) : base(services, logger, settings)
    {
    }
}
