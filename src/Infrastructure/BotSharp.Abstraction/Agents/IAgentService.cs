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
    Task<string> RefreshAgents(IEnumerable<string>? agentIds = null);
    Task<PagedItems<Agent>> GetAgents(AgentFilter filter);
    Task<List<IdName>> GetAgentOptions(List<string>? agentIds = null, bool byName = false);
    Task<IEnumerable<AgentUtility>> GetAgentUtilityOptions();

    /// <summary>
    /// Load agent configurations and trigger hooks
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<Agent> LoadAgent(string id, bool loadUtility = true);

    /// <summary>
    /// Inherit from an agent
    /// </summary>
    /// <param name="agent"></param>
    /// <returns></returns>
    Task InheritAgent(Agent agent);

    string RenderInstruction(Agent agent, Dictionary<string, object>? renderData = null);

    string RenderTemplate(Agent agent, string templateName, Dictionary<string, object>? renderData = null);

    bool RenderFunction(Agent agent, FunctionDef def, Dictionary<string, object>? renderData = null);

    FunctionParametersDef? RenderFunctionProperty(Agent agent, FunctionDef def, Dictionary<string, object>? renderData = null);

    (string, IEnumerable<FunctionDef>) PrepareInstructionAndFunctions(Agent agent, Dictionary<string, object>? renderData = null, StringComparer? comparer = null);

    bool RenderVisibility(string? visibilityExpression, Dictionary<string, object> dict);
    Dictionary<string, object> CollectRenderData(Agent agent);


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
}
