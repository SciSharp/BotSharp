using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Routing.Models;
using System.IO;
using static Tensorflow.ApiDef.Types;

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
        return JsonSerializer.Deserialize<RoutingRecord[]>(File.ReadAllText(filePath));
    }

    public RoutingRecord GetRecordByName(string name)
    {
        return GetRoutingRecords().First(x => x.Name.ToLower() == name.ToLower());
    }
}
