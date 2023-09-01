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

    public RoutingRecord[] GetRoutingRecords()
    {
        var agentSettings = _services.GetRequiredService<AgentSettings>();
        var dbSettings = _services.GetRequiredService<MyDatabaseSettings>();
        var filePath = Path.Combine(dbSettings.FileRepository, agentSettings.DataDir, _settings.RouterId, "route.json");
        var records = JsonSerializer.Deserialize<RoutingRecord[]>(File.ReadAllText(filePath));

        // check if routing profile is specified
        filePath = Path.Combine(dbSettings.FileRepository, agentSettings.DataDir, "routing-profile.json");
        if (File.Exists(filePath))
        {
            var state = _services.GetRequiredService<IConversationStateService>();
            var name = state.GetState("channel");
            var profiles = JsonSerializer.Deserialize<RoutingProfileRecord[]>(File.ReadAllText(filePath));
            var spcificedProfile = profiles.FirstOrDefault(x => x.Name == name);
            if (spcificedProfile != null)
            {
                records = records.Where(x => spcificedProfile.AgentIds.Contains(x.AgentId)).ToArray();
            }
        }

        return records;
    }

    public RoutingRecord GetRecordByName(string name)
    {
        return GetRoutingRecords().First(x => x.Name.ToLower() == name.ToLower());
    }
}
