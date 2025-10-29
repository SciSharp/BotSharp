using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Agents.Options;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Utilities;

namespace BotSharp.Plugin.Google.Core
{
    public class TestAgentService : IAgentService
    {
        public Task<Agent> CreateAgent(Agent agent)
        {
            return Task.FromResult(new Agent());
        }

        public Task<string> RefreshAgents(IEnumerable<string>? agentIds = null)
        {
            return Task.FromResult("Refreshed successfully");
        }

        public Task<PagedItems<Agent>> GetAgents(AgentFilter filter)
        {
            return Task.FromResult(new PagedItems<Agent>());
        }

        public Task<List<IdName>> GetAgentOptions(List<string>? agentIds = null, bool byName = false)
        {
            return Task.FromResult(new List<IdName> { new IdName(id: "1", name: "Fake Agent") });
        }

        public Task<Agent> LoadAgent(string id, bool loadUtility = true)
        {
            return Task.FromResult(new Agent());
        }

        public Task InheritAgent(Agent agent)
        {
            return Task.CompletedTask;
        }

        public string RenderInstruction(Agent agent, IDictionary<string, object>? renderData = null)
        {
            return "Fake Instruction";
        }

        public string RenderTemplate(Agent agent, string templateName, IDictionary<string, object>? renderData = null)
        {
            return $"Rendered template for {templateName}";
        }

        public bool RenderFunction(Agent agent, FunctionDef def, IDictionary<string, object>? renderData = null)
        {
            return true;
        }

        public (string, IEnumerable<FunctionDef>) PrepareInstructionAndFunctions(Agent agent, IDictionary<string, object>? renderData = null, StringComparer? comparer = null)
        {
            return (string.Empty, []);
        }

        public FunctionParametersDef? RenderFunctionProperty(Agent agent, FunctionDef def, IDictionary<string, object>? renderData = null)
        {
            return def.Parameters;
        }

        public bool RenderVisibility(string? visibilityExpression, IDictionary<string, object> dict)
        {
            return true;
        }

        public Task<Agent> GetAgent(string id)
        {
            return Task.FromResult(new Agent());
        }

        public Task<bool> DeleteAgent(string id, AgentDeleteOptions? options = null)
        {
            return Task.FromResult(true);
        }

        public Task UpdateAgent(Agent agent, AgentField updateField)
        {
            return Task.CompletedTask;
        }

        public Task<string> PatchAgentTemplate(Agent agent)
        {
            return Task.FromResult("Patched successfully");
        }

        public Task<string> UpdateAgentFromFile(string id)
        {
            return Task.FromResult("Updated from file successfully");
        }

        public string GetDataDir()
        {
            return "Fake Data Directory";
        }

        public string GetAgentDataDir(string agentId)
        {
            return $"Fake Data Directory for agent {agentId}";
        }

        public Task<List<UserAgent>> GetUserAgents(string userId)
        {
            return Task.FromResult(new List<UserAgent> { new UserAgent() });
        }

        public PluginDef GetPlugin(string agentId)
        {
            return new PluginDef();
        }

        public Task<IEnumerable<AgentUtility>> GetAgentUtilityOptions()
        {
            return Task.FromResult(Enumerable.Empty<AgentUtility>());
        }

        public IDictionary<string, object> CollectRenderData(Agent agent)
        {
            return new Dictionary<string, object>();
        }
    }
}