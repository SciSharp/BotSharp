using BotSharp.Abstraction.Tasks.Models;
using System.IO;
using System.Text.RegularExpressions;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task<Agent> CreateAgent(Agent agent)
    {
        var agentRecord = _db.GetAgentsByUser(_user.Id).FirstOrDefault(x => x.Name.IsEqualTo(agent.Name));

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
                       .SetAgentType(foundAgent.Type)
                       .SetProfiles(foundAgent.Profiles)
                       .SetRoutingRules(foundAgent.RoutingRules)
                       .SetInstruction(foundAgent.Instruction)
                       .SetTemplates(foundAgent.Templates)
                       .SetFunctions(foundAgent.Functions)
                       .SetResponses(foundAgent.Responses)
                       .SetLlmConfig(foundAgent.LlmConfig);
        }

        var user = _db.GetUserById(_user.Id);
        var userAgentRecord = new UserAgent
        {
            Id = Guid.NewGuid().ToString(),
            UserId = user.Id,
            AgentId = foundAgent?.Id ?? agentRecord.Id,
            Editable = false,
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow
        };

        _db.Transaction<IBotSharpTable>(delegate
        {
            _db.Add<IBotSharpTable>(agentRecord);
            _db.Add<IBotSharpTable>(userAgentRecord);
        });

        Utilities.ClearCache();

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
            var splitIdx = fileName.LastIndexOf(".");
            var name = fileName.Substring(0, splitIdx);
            var extension = fileName.Substring(splitIdx + 1);
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

    private List<string> FetchSamplesFromFile(string fileDir)
    {
        var file = Path.Combine(fileDir, "samples.txt");
        if (!File.Exists(file)) return new List<string>();

        var samples = File.ReadAllLines(file);
        return samples?.ToList() ?? new List<string>();
    }

    private List<AgentTask> FetchTasksFromFile(string fileDir)
    {
        var tasks = new List<AgentTask>();
        var taskDir = Path.Combine(fileDir, "tasks");
        if (!Directory.Exists(taskDir)) return tasks;

        var agentId = fileDir.Split(Path.DirectorySeparatorChar).Last();
        foreach (var file in Directory.GetFiles(taskDir))
        {
            var parsedTask = ParseAgentTask(file);
            if (parsedTask == null) continue;

            var task = new AgentTask
            {
                Id = parsedTask.Id,
                Name = parsedTask.Name,
                Description = parsedTask.Description,
                Enabled = parsedTask.Enabled,
                DirectAgentId = parsedTask.DirectAgentId,
                Content = parsedTask.Content,
                AgentId = agentId,
                CreatedDateTime = parsedTask.CreatedDateTime,
                UpdatedDateTime = parsedTask.UpdatedDateTime
            };
            tasks.Add(task);
        }

        return tasks;
    }

    private AgentTask? ParseAgentTask(string taskFile)
    {
        if (string.IsNullOrWhiteSpace(taskFile)) return null;

        var prefix = @"#metadata";
        var suffix = @"/metadata";
        var fileName = taskFile.Split(Path.DirectorySeparatorChar).Last();
        var id = fileName.Split('.').First();
        var data = File.ReadAllText(taskFile);
        var pattern = $@"{prefix}.+{suffix}";
        var metaData = Regex.Match(data, pattern, RegexOptions.Singleline);

        if (!metaData.Success) return null;

        var task = metaData.Value.JsonContent<AgentTask>();
        if (task == null) return null;

        task.Id = id;
        pattern = $@"{suffix}.+";
        var content = Regex.Match(data, pattern, RegexOptions.Singleline).Value;
        task.Content = content.Substring(suffix.Length).Trim();
        return task;
    }

    private UserAgent BuildUserAgent(string agentId, string userId, bool editable = false)
    {
        return new UserAgent
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            AgentId = agentId,
            Editable = editable,
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow
        };
    }
}
