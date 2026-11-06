using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Repositories.Enums;
using BotSharp.Abstraction.Repositories.Settings;
using BotSharp.Core.Repository;
using System.IO;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    /// <summary>
    /// The agents in the file repository, i.e. the ones <see cref="RefreshAgents"/> can migrate.
    /// </summary>
    public async Task<List<IdName>> GetFileAgentOptions()
    {
        var dbSettings = _services.GetRequiredService<BotSharpDatabaseSettings>();
        if (dbSettings.Default == RepositoryEnum.FileRepository)
        {
            return [];
        }

        // FileRepository rewrites the database settings it is given, so hand it a copy of the shared ones.
        var fileRepository = ActivatorUtilities.CreateInstance<FileRepository>(_services, dbSettings.DeepClone()!);
        var agents = await fileRepository.GetAgents(AgentFilter.Empty());
        return agents.Select(x => new IdName(x.Id, x.Name)).OrderBy(x => x.Name).ToList();
    }

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
        foreach (var dir in Directory.GetDirectories(agentDir))
        {
            try
            {
                var agentJson = File.ReadAllText(Path.Combine(dir, "agent.json"));
                var agent = JsonSerializer.Deserialize<Agent>(agentJson, _options.JsonSerializerOptions);
                
                if (agent == null)
                {
                    _logger.LogError($"Cannot find agent in file directory: {dir}");
                    continue;
                }

                if (!agentIds.IsNullOrEmpty() && !agentIds.Contains(agent.Id))
                {
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

                var isAgentDeleted = await _db.DeleteAgent(agent.Id, options: new()
                {
                    DeleteRoleAgents = false,
                    DeleteUserAgents = false,
                    ToDeleteCodeScripts = codeScripts
                });

                if (isAgentDeleted)
                {
                    await Task.Delay(100);
                    await _db.BulkInsertAgents([agent]);
                    await _db.BulkInsertAgentTasks(agent.Id, tasks);
                    await _db.BulkInsertAgentCodeScripts(agent.Id, codeScripts);

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
