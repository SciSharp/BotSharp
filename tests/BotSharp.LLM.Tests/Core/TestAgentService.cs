using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
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

        public Task<string> RefreshAgents()
        {
            return Task.FromResult("Refreshed successfully");
        }

        public Task<PagedItems<Agent>> GetAgents(AgentFilter filter)
        {
            return Task.FromResult(new PagedItems<Agent>());
        }

        public Task<List<IdName>> GetAgentOptions(List<string>? agentIds = null)
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

        public string RenderedInstruction(Agent agent)
        {
            return "Fake Instruction";
        }

        public string RenderedTemplate(Agent agent, string templateName)
        {
            return $"Rendered template for {templateName}";
        }

        public bool RenderFunction(Agent agent, FunctionDef def)
        {
            return true;
        }

        public FunctionParametersDef? RenderFunctionProperty(Agent agent, FunctionDef def)
        {
            return def.Parameters;
        }

        public Task<Agent> GetAgent(string id)
        {
            return Task.FromResult(new Agent());
        }

        public Task<bool> DeleteAgent(string id)
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
    }
}