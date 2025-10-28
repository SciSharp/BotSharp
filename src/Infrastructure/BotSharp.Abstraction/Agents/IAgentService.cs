using BotSharp.Abstraction.Agents.Options;
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

    string RenderInstruction(Agent agent, IDictionary<string, object>? renderData = null);

    string RenderTemplate(Agent agent, string templateName, IDictionary<string, object>? renderData = null);

    bool RenderFunction(Agent agent, FunctionDef def, IDictionary<string, object>? renderData = null);

    FunctionParametersDef? RenderFunctionProperty(Agent agent, FunctionDef def, IDictionary<string, object>? renderData = null);

    (string, IEnumerable<FunctionDef>) PrepareInstructionAndFunctions(Agent agent, IDictionary<string, object>? renderData = null, StringComparer? comparer = null);

    bool RenderVisibility(string? visibilityExpression, IDictionary<string, object> dict);
    IDictionary<string, object> CollectRenderData(Agent agent);


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
    string GetDataDir();
    string GetAgentDataDir(string agentId);

    Task<List<UserAgent>> GetUserAgents(string userId);

    PluginDef GetPlugin(string agentId);

    Task<List<AgentCodeScript>> GetAgentCodeScripts(string agentId, AgentCodeScriptFilter? filter = null)
        => Task.FromResult(new List<AgentCodeScript>());

    Task<string?> GetAgentCodeScript(string agentId, string scriptName, string scriptType = AgentCodeScriptType.Src)
        => Task.FromResult(string.Empty);

    Task<bool> UpdateAgentCodeScripts(string agentId, List<AgentCodeScript> codeScripts, AgentCodeScriptUpdateOptions? options = null)
        => Task.FromResult(false);

    Task<bool> DeleteAgentCodeScripts(string agentId, List<AgentCodeScript>? codeScripts = null)
        => Task.FromResult(false);
}
