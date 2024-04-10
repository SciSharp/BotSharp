using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Repositories.Filters;

namespace BotSharp.Abstraction.Agents;

/// <summary>
/// Agent management service
/// </summary>
public interface IAgentService
{
    Task<Agent> CreateAgent(Agent agent);
    Task RefreshAgents();
    Task<PagedItems<Agent>> GetAgents(AgentFilter filter);

    /// <summary>
    /// Load agent configurations and trigger hooks
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<Agent> LoadAgent(string id);

    string RenderedInstruction(Agent agent);

    string RenderedTemplate(Agent agent, string templateName);

    bool RenderFunction(Agent agent, FunctionDef def);

    FunctionParametersDef? RenderFunctionProperty(Agent agent, FunctionDef def);

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

    PluginDef GetPlugin(string agentId);
}
