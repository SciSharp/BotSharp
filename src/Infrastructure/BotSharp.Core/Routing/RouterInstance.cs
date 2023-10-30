using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Planning;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing;

public class RouterInstance : IRouterInstance
{
    protected readonly IServiceProvider _services;
    protected readonly ILogger _logger;
    protected readonly RoutingSettings _settings;

    private Agent _router;
    public Agent Router => _router;
    public virtual string AgentId => _router.Id;

    public RouterInstance(IServiceProvider services,
        ILogger<RouterInstance> logger,
        RoutingSettings settings)
    {
        _services = services;
        _logger = logger;
        _settings = settings;
    }

    public IRouterInstance Load()
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        _router = agentService.LoadAgent(_settings.RouterId).Result;
        return this;
    }

    public List<RoutingHandlerDef> GetHandlers()
    {
        var planer = _services.GetRequiredService<IPlaner>();

        return _services.GetServices<IRoutingHandler>()
            .Where(x => x.Planers == null || x.Planers.Contains(planer.GetType().Name))
            .Where(x => !string.IsNullOrEmpty(x.Description))
            .Select((x, i) => new RoutingHandlerDef
            {
                Name = x.Name,
                Description = x.Description,
                Parameters = x.Parameters
            }).ToList();
    }

#if !DEBUG
    [MemoryCache(10 * 60)]
#endif
    protected RoutingRule[] GetRoutingRecords()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();

        var agents = db.GetAgents(disabled: false, allowRouting: true);
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

#if !DEBUG
    [MemoryCache(10 * 60)]
#endif
    public RoutingItem[] GetRoutingItems()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();

        var agents = db.GetAgents(disabled: false, allowRouting: true);
        return agents.Select(x => new RoutingItem
        {
            AgentId = x.Id,
            Description = x.Description,
            Name = x.Name,
            RequiredFields = x.RoutingRules
                .Where(p => p.Required)
                .Select(p => new ParameterPropertyDef(p.Field, p.Description, type: p.Type)
                {
                    Required = p.Required
                }).ToList(),
            OptionalFields = x.RoutingRules
                .Where(p => !p.Required)
                .Select(p => new ParameterPropertyDef(p.Field, p.Description, type: p.Type)
                {
                    Required = p.Required
                }).ToList()
        }).ToArray();
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
