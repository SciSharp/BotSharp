using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing;

public class Router : IAgentRouting
{
    protected readonly IServiceProvider _services;
    protected readonly ILogger _logger;
    protected readonly RoutingSettings _settings;

    public virtual string AgentId => _settings.RouterId;

    public Router(IServiceProvider services,
        ILogger<Router> logger,
        RoutingSettings settings)
    {
        _services = services;
        _logger = logger;
        _settings = settings;
    }

    public virtual async Task<Agent> LoadRouter()
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        return await agentService.LoadAgent(AgentId);
    }

#if !DEBUG
    [MemoryCache(10 * 60)]
#endif
    protected RoutingRule[] GetRoutingRecords()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();

        var agents = db.Agents.Where(x => !x.Disabled && x.AllowRouting).ToArray();
        var records = agents.SelectMany(x =>
        {
            x.RoutingRules.ForEach(r =>
            {
                r.AgentId = x.Id;
                r.AgentName = x.Name;
            });
            return x.RoutingRules;
        }).ToArray();

        // Filter agents by profile
        var state = _services.GetRequiredService<IConversationStateService>();
        var name = state.GetState("channel");
        var specifiedProfile = agents.FirstOrDefault(x => x.Profiles.Contains(name));
        if (specifiedProfile != null)
        {
            records = records.Where(x => specifiedProfile.Profiles.Contains(name)).ToArray();
        }

        return records;
    }

    public RoutingRule[] GetRulesByName(string name)
    {
        return GetRoutingRecords()
            .Where(x => x.AgentName.ToLower() == name.ToLower())
            .ToArray();
    }

    public RoutingRule[] GetRulesByAgentId(string id)
    {
        return GetRoutingRecords()
            .Where(x => x.AgentId == id)
            .ToArray();
    }
}
