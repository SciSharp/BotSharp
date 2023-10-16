using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Utilities;
using System.IO;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task<Agent> CreateAgent(Agent agent)
    {
        var agentRecord = (from a in _db.Agents
                     join ua in _db.UserAgents on a.Id equals ua.AgentId
                     join u in _db.Users on ua.UserId equals u.Id
                     where u.ExternalId == _user.Id && a.Name == agent.Name
                     select a).FirstOrDefault();

        if (agentRecord != null)
        {
            return agentRecord;
        }

        agentRecord = Agent.Clone(agent);
        agentRecord.Id = Guid.NewGuid().ToString();
        agentRecord.CreatedDateTime = DateTime.UtcNow;
        agentRecord.UpdatedDateTime = DateTime.UtcNow;

        var dbSettings = _services.GetRequiredService<BotSharpDatabaseSettings>();
        var agentSettings = _services.GetRequiredService<AgentSettings>();
        var filePath = Path.Combine(dbSettings.FileRepository, agentSettings.DataDir);
        var foundAgent = FetchAgentFileByName(agent.Name, filePath);

        if (foundAgent != null)
        {
            agentRecord.SetId(foundAgent.Id)
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
                       .SetResponses(foundAgent.Responses);
        }

        var user = _db.Users.FirstOrDefault(x => x.Id == _user.Id || x.ExternalId == _user.Id);
        var userAgentRecord = new UserAgent
        {
            Id = Guid.NewGuid().ToString(),
            UserId = user.Id,
            AgentId = foundAgent?.Id ?? agentRecord.Id,
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow
        };

        _db.Transaction<IBotSharpTable>(delegate
        {
            _db.Add<IBotSharpTable>(agentRecord);
            _db.Add<IBotSharpTable>(userAgentRecord);
        });

        return agentRecord;
    }

    private Agent FetchAgentFileByName(string agentName, string filePath)
    {
        foreach (var dir in Directory.GetDirectories(filePath))
        {
            var agentJson = File.ReadAllText(Path.Combine(dir, "agent.json"));
            var agent = JsonSerializer.Deserialize<Agent>(agentJson, _options);
            if (agent != null && agent.Name.IsEqualTo(agentName))
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

    private string FetchInstructionFromFile(string fileDir)
    {
        var file = Path.Combine(fileDir, $"instruction.{_agentSettings.TemplateFormat}");
        if (!File.Exists(file)) return null;

        var instruction = File.ReadAllText(file);
        return instruction;
    }

    private List<AgentTemplate> FetchTemplatesFromFile(string fileDir)
    {
        var templates = new List<AgentTemplate>();
        var templateDir = Path.Combine(fileDir, "templates");
        if (!Directory.Exists(templateDir)) return templates;

        foreach (var file in Directory.GetFiles(templateDir))
        {
            var fileName = file.Split(Path.DirectorySeparatorChar).Last();
            var splits = fileName.ToLower().Split('.');
            var name = splits[0];
            var extension = splits[1];
            if (extension.IsEqualTo(_agentSettings.TemplateFormat))
            {
                var content = File.ReadAllText(file);
                templates.Add(new AgentTemplate(name, content));
            }
        }
        
        return templates;
    }

    private List<FunctionDef> FetchFunctionsFromFile(string fileDir)
    {
        var file = Path.Combine(fileDir, "functions.json");
        if (!File.Exists(file)) return new List<FunctionDef>();

        var functionsJson = File.ReadAllText(file);
        var functions = JsonSerializer.Deserialize<List<FunctionDef>>(functionsJson, _options);
        return functions;
    }

    private List<AgentResponse> FetchResponsesFromFile(string fileDir)
    {
        var responses = new List<AgentResponse>();
        var responseDir = Path.Combine(fileDir, "responses");
        if (!Directory.Exists(responseDir)) return responses;

        foreach (var file in Directory.GetFiles(responseDir))
        {
            var fileName = file.Split(Path.DirectorySeparatorChar).Last();
            var splits = fileName.Split('.');
            var prefix = splits[0];
            var intent = splits[1];
            var content = File.ReadAllText(file);
            responses.Add(new AgentResponse(prefix, intent, content));
        }
        return responses;
    }
}
