using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Abstraction.Agents;

/// <summary>
/// Agent management service
/// </summary>
public interface IAgentService
{
    Task<string> CreateAgent(Agent agent);
    Task<bool> DeleteAgent(string id);
    Task UpdateAgent(Agent agent);
}
