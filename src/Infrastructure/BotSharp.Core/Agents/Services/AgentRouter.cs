using BotSharp.Abstraction.Agents.Models;

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

    public async Task<Agent> LoadCurrentAgent()
    {
        // Load current agent from state
        var stateService = _services.GetRequiredService<IConversationStateService>();
        var currentAgentId = stateService.GetState("agentId");
        if (string.IsNullOrEmpty(currentAgentId))
        {
            currentAgentId = _settings.RouterId;
        }
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(currentAgentId);

        // Set agent and trigger state changed
        stateService.SetState("agentId", currentAgentId);

        return agent;
    }
}
