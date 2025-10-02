using BotSharp.Abstraction.Repositories.Enums;
using BotSharp.Abstraction.Repositories.Settings;
using System.IO;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task<string> RefreshAgents(IEnumerable<string>? agentIds = null)
    {
        string refreshResult;
        var dbSettings = _services.GetRequiredService<BotSharpDatabaseSettings>();
        if (dbSettings.Default == RepositoryEnum.FileRepository)
        {
            refreshResult = $"Invalid database repository setting: {dbSettings.Default}";
            _logger.LogWarning(refreshResult);
            return refreshResult;
        }

        var agentDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                    dbSettings.FileRepository,
                                    _agentSettings.DataDir);

        if (!Directory.Exists(agentDir))
        {
            refreshResult = $"Cannot find the directory: {agentDir}";
            return refreshResult;
        }
        
        var refreshedAgents = new List<string>();
        var dirs = Directory.GetDirectories(agentDir);
        if (!agentIds.IsNullOrEmpty())
        {
            dirs = dirs.Where(x => agentIds.Contains(x.Split(Path.DirectorySeparatorChar).Last())).ToArray();
        }

        foreach (var dir in dirs)
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
                var codeScripts = GetCodeScriptsFromFile(dir);

                var isAgentDeleted = _db.DeleteAgent(agent.Id);
                if (isAgentDeleted)
                {
                    await Task.Delay(100);
                    _db.BulkInsertAgents([agent]);
                    _db.BulkInsertAgentTasks(agent.Id, tasks);
                    _db.BulkInsertAgentCodeScripts(agent.Id, codeScripts);

                    refreshedAgents.Add(agent.Name);
                    _logger.LogInformation($"Agent {agent.Name} has been migrated.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to migrate agent in file directory: {dir}\r\nError: {ex.Message}");
            }
        }

        if (!refreshedAgents.IsNullOrEmpty())
        {
            Utilities.ClearCache();
            refreshResult = $"Agents are migrated!\r\n{string.Join("\r\n", refreshedAgents)}";
        }
        else
        {
            refreshResult = "No agent gets migrated!";
        }

        _logger.LogInformation(refreshResult);
        return refreshResult;
    }
}
