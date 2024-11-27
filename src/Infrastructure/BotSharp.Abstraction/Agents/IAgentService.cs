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
    Task<string> RefreshAgents();
    Task<PagedItems<Agent>> GetAgents(AgentFilter filter);

    /// <summary>
    /// Load agent configurations and trigger hooks
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<Agent> LoadAgent(string id);

    /// <summary>
    /// Inherit from an agent
    /// </summary>
    /// <param name="agent"></param>
    /// <returns></returns>
    Task InheritAgent(Agent agent);

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

    /// <summary>
    /// Path existing templates of agent, cannot create new or delete templates
    /// </summary>
    /// <param name="agent"></param>
    /// <returns></returns>
    Task<string> PatchAgentTemplate(Agent agent);
    Task<string> UpdateAgentFromFile(string id);
    string GetDataDir();
    string GetAgentDataDir(string agentId);

    Task<List<UserAgent>> GetUserAgents(string userId);

    PluginDef GetPlugin(string agentId);

    IEnumerable<AgentUtility> GetAgentUtilityOptions();
}
