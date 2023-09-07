using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Settings;
using System.IO;

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

    public RoutingItem[] GetRoutingRecords()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();

        var records = db.RoutingItems.ToArray();
        var profiles = db.RoutingProfiles.ToList();

        if (!profiles.IsNullOrEmpty())
        {
            var state = _services.GetRequiredService<IConversationStateService>();
            var name = state.GetState("channel");
            var specifiedProfile = profiles.FirstOrDefault(x => x.Name == name);
            if (specifiedProfile != null)
            {
                records = records.Where(x => specifiedProfile.AgentIds.Contains(x.AgentId)).ToArray();
            }
        }

        return records;
    }

    public RoutingItem GetRecordByName(string name)
    {
        return GetRoutingRecords().First(x => x.Name.ToLower() == name.ToLower());
    }
}
