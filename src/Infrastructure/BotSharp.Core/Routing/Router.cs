using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing.Models;
using System.IO;

namespace BotSharp.Core.Routing;

public class Router : IAgentRouting
{
    protected readonly IServiceProvider _services;
    protected readonly ILogger _logger;
    protected readonly AgentSettings _settings;

    public virtual string AgentId => _settings.RouterId;

    public Router(IServiceProvider services,
        ILogger<Router> logger,
        AgentSettings settings)
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
        var filePath = Path.Combine(dbSettings.FileRepository, agentSettings.DataDir, agentSettings.RouterId, "route.json");

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var agent = db.Agent.FirstOrDefault(x => x.Id == agentSettings.RouterId);
        var routes = agent?.Routes ?? new List<string>();
        var routingRecords = new RoutingRecord[routes.Count];

        for (int i = 0; i < routes.Count; i++)
        {
            if (string.IsNullOrEmpty(routes[i])) continue;
            routingRecords[i] = JsonSerializer.Deserialize<RoutingRecord>(routes[i]);
        }

        return routingRecords;
    }

    public RoutingRecord GetRecordByName(string name)
    {
        return GetRoutingRecords().First(x => x.Name.ToLower() == name.ToLower());
    }
}
