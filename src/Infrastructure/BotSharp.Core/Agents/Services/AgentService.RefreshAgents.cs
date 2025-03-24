using BotSharp.Abstraction.Repositories.Enums;
using BotSharp.Abstraction.Tasks.Models;
using System.IO;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task<string> RefreshAgents()
    {
        string refreshResult;
        var dbSettings = _services.GetRequiredService<BotSharpDatabaseSettings>();
        if (dbSettings.Default == RepositoryEnum.FileRepository)
        {
            refreshResult = $"Invalid database repository setting: {dbSettings.Default}";
            _logger.LogWarning(refreshResult);
            return refreshResult;
        }

        var userIdentity = _services.GetRequiredService<IUserIdentity>();
        var userService = _services.GetRequiredService<IUserService>();
        var (isValid, _) = await userService.IsAdminUser(userIdentity.Id);
        if (!isValid)
        {
            return "Unauthorized user.";
        }

        var agentDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbSettings.FileRepository, _agentSettings.DataDir);
        if (!Directory.Exists(agentDir))
        {
            refreshResult = $"Cannot find the directory: {agentDir}";
            return refreshResult;
        }

        List<Agent> agents = [];
        List<AgentTask> agentTasks = [];

        foreach (var dir in Directory.GetDirectories(agentDir))
        {
            try
            {
                var (agent, msg) = GetAgentFormJson(Path.Combine(dir, "agent.json"));
                if (agent == null)
                {
                    _logger.LogError(msg);
                    continue;
                }

                var (defaultInstruction, channelInstructions) = GetInstructionsFromFile(dir);
                var functions = GetFunctionsFromFile(dir);
                var responses = GetResponsesFromFile(dir);
                var templates = GetTemplatesFromFile(dir);
                var samples = GetSamplesFromFile(dir);
                agent.SetInstruction(defaultInstruction)
                     .SetChannelInstructions(channelInstructions)
                     .SetTemplates(templates)
                     .SetFunctions(functions)
                     .SetResponses(responses)
                     .SetSamples(samples);

                var tasks = GetTasksFromFile(dir);
                if (!tasks.IsNullOrEmpty()) agentTasks.AddRange(tasks);
                agents.Add(agent);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to migrate agent in file directory: {dir}\r\nError: {ex.Message}");
            }
        }

        if (agents.Count > 0)
        {
            var agentIds = agents.Select(a => a.Id).ToList();
            await _db.DeleteAgentsAsync(agentIds);
            await Task.Delay(200);
            await _db.BulkInsertAgentsAsync(agents);
            await Task.Delay(200);
            await _db.BulkInsertAgentTasksAsync(agentTasks);

            Utilities.ClearCache();
            refreshResult = $"Agents are migrated!\r\n{string.Join("\r\n", agents.Select(a => a.Name))}";
        }
        else
        {
            refreshResult = "No agent gets refreshed!";
        }

        _logger.LogInformation(refreshResult);
        return refreshResult;
    }

    private (Agent? agent, string msg) GetAgentFormJson(string agentPath)
    {
        var agentJson = File.ReadAllText(agentPath);
        if (string.IsNullOrWhiteSpace(agentJson))
            return (null, $"Cannot find agent in file path: {agentPath}");

        var isJson = IsValidedJson(agentJson);
        if (isJson)
        {
            var agent = JsonSerializer.Deserialize<Agent>(agentJson, _options);
            return (agent, "ok");
        }
        else
        {
            return (null, "The agent.json file data is not in JSON format!");
        }
    }

    private bool IsValidedJson(string jsonString)
    {
        try
        {
            JsonDocument.Parse(jsonString);
            return true;
        }
        catch (JsonException ex)
        {
            return false;
        }
    }

}
