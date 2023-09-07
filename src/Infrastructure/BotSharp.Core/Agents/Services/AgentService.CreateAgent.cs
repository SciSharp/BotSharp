using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Repositories;
using System.IO;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task<Agent> CreateAgent(Agent agent)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();

        var agentRecord = (from a in db.Agents
                     join ua in db.UserAgents on a.Id equals ua.AgentId
                     join u in db.Users on ua.UserId equals u.Id
                     where u.ExternalId == _user.Id && a.Name == agent.Name
                     select a).FirstOrDefault();

        if (agentRecord != null)
        {
            return agentRecord;
        }

        agentRecord = Agent.Clone(agent);
        agentRecord.Id = Guid.NewGuid().ToString();
        agentRecord.CreatedTime = DateTime.UtcNow;
        agentRecord.UpdatedTime = DateTime.UtcNow;

        var dbSettings = _services.GetRequiredService<BotSharpDatabaseSettings>();
        var agentSettings = _services.GetRequiredService<AgentSettings>();
        var filePath = Path.Combine(dbSettings.FileRepository, agentSettings.DataDir);
        var foundAgent = FetchAgentInfoFromFile(agent.Name, filePath);

        if (foundAgent != null)
        {
            agentRecord.SetId(foundAgent.Id)
                       .SetName(foundAgent.Name)
                       .SetDescription(foundAgent.Description)
                       .SetIsPublic(foundAgent.IsPublic)
                       .SetInstruction(foundAgent.Instruction)
                       .SetFunctions(foundAgent.Functions)
                       .SetResponses(foundAgent.Responses);
        }

        var user = db.Users.FirstOrDefault(x => x.ExternalId == _user.Id);
        var userAgentRecord = new UserAgent
        {
            Id = Guid.NewGuid().ToString(),
            UserId = user.Id,
            AgentId = foundAgent?.Id ?? agentRecord.Id,
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow
        };

        db.Transaction<IBotSharpTable>(delegate
        {
            db.Add<IBotSharpTable>(agentRecord);
            db.Add<IBotSharpTable>(userAgentRecord);
        });

        return agentRecord;
    }

    private JsonSerializerOptions _options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private Agent FetchAgentInfoFromFile(string agentName, string filePath)
    {
        foreach (var dir in Directory.GetDirectories(filePath))
        {
            var agentJson = File.ReadAllText(Path.Combine(dir, "agent.json"));
            var agent = JsonSerializer.Deserialize<Agent>(agentJson, _options);
            if (agent != null && agent.Name == agentName)
            {
                var functions = FetchFunctionsFromFile(dir);
                var instruction = FetchInstructionFromFile(dir);
                var responses = FetchResponsesFromFile(dir);
                return agent.SetInstruction(instruction)
                            .SetFunctions(functions)
                            .SetResponses(responses);
            }
        }

        return null;
    }

    private string FetchInstructionFromFile(string fileDir)
    {
        var file = Path.Combine(fileDir, "instruction.liquid");
        if (!File.Exists(file)) return null;

        var instruction = File.ReadAllText(file);
        return instruction;
    }

    private List<string> FetchFunctionsFromFile(string fileDir)
    {
        var file = Path.Combine(fileDir, "functions.json");
        if (!File.Exists(file)) return new List<string>();

        var functionsJson = File.ReadAllText(file);
        var functionDefs = JsonSerializer.Deserialize<List<Abstraction.Functions.Models.FunctionDef>>(functionsJson, _options);
        var functions = functionDefs.Select(x => JsonSerializer.Serialize(x, _options)).ToList();
        return functions;
    }

    private List<string> FetchResponsesFromFile(string fileDir)
    {
        var responses = new List<string>();
        var responseDir = Path.Combine(fileDir, "responses");
        if (!Directory.Exists(responseDir)) return responses;

        foreach (var file in Directory.GetFiles(responseDir))
        {
            responses.Add(File.ReadAllText(file));
        }
        return responses;
    }
}
