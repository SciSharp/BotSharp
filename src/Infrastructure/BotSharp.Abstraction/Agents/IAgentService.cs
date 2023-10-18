namespace BotSharp.Abstraction.Agents;

/// <summary>
/// Agent management service
/// </summary>
public interface IAgentService
{
    Task<Agent> CreateAgent(Agent agent);
    Task RefreshAgents();
    Task<List<Agent>> GetAgents();

    /// <summary>
    /// Load agent configurations and trigghe hooks
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<Agent> LoadAgent(string id);

    /// <summary>
    /// Get agent detail without trigger any hook.
    /// </summary>
    /// <param name="id"></param>
    /// <returns>Original agent information</returns>
    Task<Agent> GetAgent(string id);

    Task<bool> DeleteAgent(string id);
    Task UpdateAgent(Agent agent, AgentField updateField);
    Task UpdateAgentFromFile(string id);
    string GetDataDir();
    string GetAgentDataDir(string agentId);
}
