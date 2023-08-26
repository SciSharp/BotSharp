using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Repositories;
using System.IO;

namespace BotSharp.Core.Agents.Services;

public class AgentRouter : IAgentRouting
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly AgentSettings _settings;

    public AgentRouter(IServiceProvider services, 
        ILogger<AgentRouter> logger,
        AgentSettings settings)
    {
        _services = services;
        _logger = logger;
        _settings = settings;
    }

    public async Task<Agent> LoadRouter()
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(_settings.RouterId);
        return agent;
    }

    public async Task<Agent> LoadCurrentAgent()
    {
        // Load current agent from state
        var state = _services.GetRequiredService<IConversationStateService>();
        var currentAgentId = state.GetState("agent_id");
        if (string.IsNullOrEmpty(currentAgentId))
        {
            currentAgentId = _settings.RouterId;
        }
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(currentAgentId);

        // Set agent and trigger state changed
        state.SetState("agent_id", currentAgentId);

        return agent;
    }

    public RoutingRecord[] GetRoutingRecords()
    {
        var agentSettings = _services.GetRequiredService<AgentSettings>();
        var dbSettings = _services.GetRequiredService<MyDatabaseSettings>();
        var filePath = Path.Combine(dbSettings.FileRepository, agentSettings.DataDir, agentSettings.RouterId, "route.json");
        return JsonSerializer.Deserialize<RoutingRecord[]>(File.ReadAllText(filePath));
    }
}
