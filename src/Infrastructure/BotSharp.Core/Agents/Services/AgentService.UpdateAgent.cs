using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Repositories;
using System.IO;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task UpdateAgent(Agent agent)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();

        var record = (from a in db.Agents
                      join ua in db.UserAgents on a.Id equals ua.AgentId
                      join u in db.Users on ua.UserId equals u.Id
                      where (ua.UserId == _user.Id || u.ExternalId == _user.Id) &&
                        a.Id == agent.Id
                      select a).FirstOrDefault();

        if (record == null) return;

        record.Name = agent.Name;

        if (!string.IsNullOrEmpty(agent.Description))
            record.Description = agent.Description;

        if (!string.IsNullOrEmpty(agent.Instruction))
            record.Instruction = agent.Instruction;

        if (!agent.Templates.IsNullOrEmpty())
            record.Templates = agent.Templates;

        if (!agent.Functions.IsNullOrEmpty())
            record.Functions = agent.Functions;

        if (!agent.Responses.IsNullOrEmpty())
            record.Responses = agent.Responses;

        db.UpdateAgent(record);
        await Task.CompletedTask;
    }

    public async Task UpdateAgentFromFile(string id)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var agent = db.Agents?.FirstOrDefault(x => x.Id == id);

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

            db.UpdateAgent(clonedAgent);
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
