using System.IO;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task<string> RefreshAgents()
    {
        var dbSettings = _services.GetRequiredService<BotSharpDatabaseSettings>();
        var agentDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                    dbSettings.FileRepository,
                                    _agentSettings.DataDir);

        string refreshResult;
        if (!Directory.Exists(agentDir))
        {
            refreshResult = $"Cannot find the directory: {agentDir}";
            return refreshResult;
        }

        var user = _db.GetUserById(_user.Id);
        var refreshedAgents = new List<string>();

        foreach (var dir in Directory.GetDirectories(agentDir))
        {
            try
            {
                var agentJson = File.ReadAllText(Path.Combine(dir, "agent.json"));
                var agent = JsonSerializer.Deserialize<Agent>(agentJson, _options);
                
                if (agent == null)
                {
                    _logger.LogError($"Cannot find agent in file directory: {dir}");
                    continue;
                }

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

                var userAgent = BuildUserAgent(agent.Id, user.Id);
                var tasks = FetchTasksFromFile(dir);

                var isAgentDeleted = _db.DeleteAgent(agent.Id);
                if (isAgentDeleted)
                {
                    _db.BulkInsertAgents(new List<Agent> { agent });
                    _db.BulkInsertUserAgents(new List<UserAgent> { userAgent });
                    _db.BulkInsertAgentTasks(tasks);
                    refreshedAgents.Add(agent.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to migrate agent in file directory: {dir}\r\nError: {ex.Message}");
            }
        }

        if (!refreshedAgents.IsNullOrEmpty())
        {
            Utilities.ClearCache();
            refreshResult = $"Agents are migrated! {string.Join("\r\n", refreshedAgents)}";
        }
        else
        {
            refreshResult = "No agent gets refreshed!";
        }

        _logger.LogInformation(refreshResult);
        return refreshResult;
    }
}
