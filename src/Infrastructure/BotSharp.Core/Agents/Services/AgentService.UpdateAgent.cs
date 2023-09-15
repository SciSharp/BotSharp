using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Repositories;
using System.IO;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task UpdateAgent(Agent agent, AgentField updateField)
    {
        if (agent == null || string.IsNullOrEmpty(agent.Id)) return;

        var record = FindAgent(agent.Id);
        if (record == null) return;

        _db.UpdateAgent(record, updateField);
        await Task.CompletedTask;
    }

    private Agent FindAgent(string agentId)
    {
        var record = (from a in _db.Agents
                      join ua in _db.UserAgents on a.Id equals ua.AgentId
                      join u in _db.Users on ua.UserId equals u.Id
                      where (ua.UserId == _user.Id || u.ExternalId == _user.Id) &&
                        a.Id == agentId
                      select a).FirstOrDefault();
        return record;
    }

    public async Task UpdateAgentFromFile(string id)
    {
        var agent = _db.Agents?.FirstOrDefault(x => x.Id == id);

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
                       .SetInstruction(foundAgent.Instruction)
                       .SetTemplates(foundAgent.Templates)
                       .SetFunctions(foundAgent.Functions)
                       .SetResponses(foundAgent.Responses);

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
                return agent.SetInstruction(instruction)
                            .SetTemplates(templates)
                            .SetFunctions(functions)
                            .SetResponses(responses);
            }
        }

        return null;
    }
}
