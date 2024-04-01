using BotSharp.Abstraction.Tasks.Models;
using System.IO;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task RefreshAgents()
    {
        var isAgentDeleted = _db.DeleteAgents();
        var isTaskDeleted = _db.DeleteAgentTasks();
        if (!isAgentDeleted) return;

        var dbSettings = _services.GetRequiredService<BotSharpDatabaseSettings>();
        var agentDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                    dbSettings.FileRepository,
                                    _agentSettings.DataDir);

        var user = _db.GetUserById(_user.Id);
        var agents = new List<Agent>();
        var userAgents = new List<UserAgent>();
        var agentTasks = new List<AgentTask>();

        foreach (var dir in Directory.GetDirectories(agentDir))
        {
            var agentJson = File.ReadAllText(Path.Combine(dir, "agent.json"));
            var agent = JsonSerializer.Deserialize<Agent>(agentJson, _options);
            if (agent == null) continue;

            var functions = FetchFunctionsFromFile(dir);
            var instruction = FetchInstructionFromFile(dir);
            var responses = FetchResponsesFromFile(dir);
            var templates = FetchTemplatesFromFile(dir);
            var samples = FetchSamplesFromFile(dir);
            agent.SetInstruction(instruction)
                 .SetTemplates(templates)
                 .SetFunctions(functions)
                 .SetResponses(responses)
                 .SetSamples(samples);
            agents.Add(agent);

            var userAgent = BuildUserAgent(agent.Id, user.Id);
            userAgents.Add(userAgent);

            var tasks = FetchTasksFromFile(dir);
            agentTasks.AddRange(tasks);
        }

        _db.BulkInsertAgents(agents);
        _db.BulkInsertUserAgents(userAgents);
        _db.BulkInsertAgentTasks(agentTasks);

        Utilities.ClearCache();
    }
}
