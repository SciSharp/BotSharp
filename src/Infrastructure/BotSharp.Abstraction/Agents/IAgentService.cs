using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Abstraction.Agents;

/// <summary>
/// Agent management service
/// </summary>
public interface IAgentService
{
    Task<Agent> CreateAgent(Agent agent);
    Task<List<Agent>> GetAgents();
    Task<bool> DeleteAgent(string id);
    Task UpdateAgent(Agent agent);
}
