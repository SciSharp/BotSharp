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

        var user = _db.GetUserById(_user.Id);
        var userAgentRecord = new UserAgent
        {
            Id = Guid.NewGuid().ToString(),
            UserId = user.Id,
            AgentId = agentRecord.Id,
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
        return await Task.FromResult(agentRecord);
    }

    private (string, List<ChannelInstruction>) GetInstructionsFromFile(string fileDir)
    {
        var defaultInstruction = string.Empty;
        var channelInstructions = new List<ChannelInstruction>();

        var instructionDir = Path.Combine(fileDir, "instructions");
        if (!Directory.Exists(instructionDir))
        {
            return (defaultInstruction, channelInstructions);
        }

        foreach (var file in Directory.GetFiles(instructionDir))
        {
            var extension = Path.GetExtension(file).Substring(1);
            if (!extension.IsEqualTo(_agentSettings.TemplateFormat))
            {
                continue;
            }

            var segments = Path.GetFileName(file).Split(".", StringSplitOptions.RemoveEmptyEntries);
            if (segments.IsNullOrEmpty() || !segments[0].IsEqualTo("instruction"))
            {
                continue;
            }

            if (segments.Length == 2)
            {
                defaultInstruction = File.ReadAllText(file);
            }
            else if (segments.Length == 3)
            {
                var item = new ChannelInstruction
                {
                    Channel = segments[1],
                    Instruction = File.ReadAllText(file)
                };
                channelInstructions.Add(item);
            }
        }
        return (defaultInstruction, channelInstructions);
    }

    private List<AgentTemplate> GetTemplatesFromFile(string fileDir)
    {
        var templates = new List<AgentTemplate>();
        var templateDir = Path.Combine(fileDir, "templates");
        if (!Directory.Exists(templateDir)) return templates;

        foreach (var file in Directory.GetFiles(templateDir))
        {
            var extension = Path.GetExtension(file).Substring(1);
            if (extension.IsEqualTo(_agentSettings.TemplateFormat))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var content = File.ReadAllText(file);
                templates.Add(new AgentTemplate(name, content));
            }
        }
        
        return templates;
    }

    private List<FunctionDef> GetFunctionsFromFile(string fileDir)
    {
        var functions = new List<FunctionDef>();
        var functionDir = Path.Combine(fileDir, "functions");

        if (!Directory.Exists(functionDir)) return functions;

        foreach (var file in Directory.GetFiles(functionDir))
        {
            try
            {
                var extension = Path.GetExtension(file).Substring(1);
                if (extension != "json") continue;

                var json = File.ReadAllText(file);
                var function = JsonSerializer.Deserialize<FunctionDef>(json, _options);
                functions.Add(function);
            }
            catch
            {
                continue;
            }

        }
        return functions;
    }

    private List<AgentResponse> GetResponsesFromFile(string fileDir)
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

    private List<string> GetSamplesFromFile(string fileDir)
    {
        var file = Path.Combine(fileDir, "samples.txt");
        if (!File.Exists(file)) return new List<string>();

        var samples = File.ReadAllLines(file);
        return samples?.ToList() ?? new List<string>();
    }

    private List<AgentTask> GetTasksFromFile(string fileDir)
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
