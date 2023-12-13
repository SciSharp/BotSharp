using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing.Models;
using System.IO;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task UpdateAgent(Agent agent, AgentField updateField)
    {
        if (agent == null || string.IsNullOrEmpty(agent.Id)) return;

        var record = _db.GetAgent(agent.Id);
        if (record == null) return;

        record.Name = agent.Name ?? string.Empty;
        record.Description = agent.Description ?? string.Empty;
        record.IsPublic = agent.IsPublic;
        record.Disabled = agent.Disabled;
        record.AllowRouting = agent.AllowRouting;
        record.Profiles = agent.Profiles ?? new List<string>();
        record.RoutingRules = agent.RoutingRules ?? new List<RoutingRule>();
        record.Instruction = agent.Instruction ?? string.Empty;
        record.Functions = agent.Functions ?? new List<FunctionDef>();
        record.Templates = agent.Templates ?? new List<AgentTemplate>();
        record.Responses = agent.Responses ?? new List<AgentResponse>();
        record.Samples = agent.Samples ?? new List<string>();
        record.LlmConfig = agent.LlmConfig;

        _db.UpdateAgent(record, updateField);
        await Task.CompletedTask;
    }

    public async Task UpdateAgentFromFile(string id)
    {
        var agent = _db.GetAgent(id);

        if (agent == null) return;

        var dbSettings = _services.GetRequiredService<BotSharpDatabaseSettings>();
        var agentSettings = _services.GetRequiredService<AgentSettings>();
        var filePath = Path.Combine(dbSettings.FileRepository, agentSettings.DataDir);

        var clonedAgent = Agent.Clone(agent);
        var foundAgent = FetchAgentFileById(agent.Id, filePath);
        if (foundAgent != null)
        {
            clonedAgent.SetId(foundAgent.Id)
                       .SetName(foundAgent.Name)
                       .SetDescription(foundAgent.Description)
                       .SetIsPublic(foundAgent.IsPublic)
                       .SetDisabled(foundAgent.Disabled)
                       .SetAllowRouting(foundAgent.AllowRouting)
                       .SetProfiles(foundAgent.Profiles)
                       .SetRoutingRules(foundAgent.RoutingRules)
                       .SetInstruction(foundAgent.Instruction)
                       .SetTemplates(foundAgent.Templates)
                       .SetFunctions(foundAgent.Functions)
                       .SetResponses(foundAgent.Responses)
                       .SetSamples(foundAgent.Samples)
                       .SetLlmConfig(foundAgent.LlmConfig);

            _db.UpdateAgent(clonedAgent, AgentField.All);
        }

        await Task.CompletedTask;
    }

    private Agent FetchAgentFileById(string agentId, string filePath)
    {
        foreach (var dir in Directory.GetDirectories(filePath))
        {
            var agentJson = File.ReadAllText(Path.Combine(dir, "agent.json"));
            var agent = JsonSerializer.Deserialize<Agent>(agentJson, _options);
            if (agent != null && agent.Id == agentId)
            {
                var functions = FetchFunctionsFromFile(dir);
                var instruction = FetchInstructionFromFile(dir);
                var responses = FetchResponsesFromFile(dir);
                var templates = FetchTemplatesFromFile(dir);
                var samples = FetchSamplesFromFile(dir);
                return agent.SetInstruction(instruction)
                            .SetTemplates(templates)
                            .SetFunctions(functions)
                            .SetResponses(responses)
                            .SetSamples(samples);
            }
        }

        return null;
    }
}
