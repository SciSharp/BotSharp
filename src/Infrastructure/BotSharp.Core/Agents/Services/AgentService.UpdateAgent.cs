using BotSharp.Abstraction.Repositories.Enums;
using BotSharp.Abstraction.Users.Enums;
using BotSharp.Abstraction.Users.Models;
using System.IO;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task UpdateAgent(Agent agent, AgentField updateField)
    {
        if (agent == null || string.IsNullOrEmpty(agent.Id)) return;

        var userService = _services.GetRequiredService<IUserService>();
        var auth = await userService.GetUserAuthorizations(new List<string> { agent.Id });
        var allowEdit = auth.IsAgentActionAllowed(agent.Id, UserAction.Edit);

        if (!allowEdit)
        {
            return;
        }

        var record = _db.GetAgent(agent.Id);
        if (record == null) return;

        record.Name = agent.Name ?? string.Empty;
        record.Description = agent.Description ?? string.Empty;
        record.IsPublic = agent.IsPublic;
        record.Disabled = agent.Disabled;
        record.MergeUtility = agent.MergeUtility;
        record.MaxMessageCount = agent.MaxMessageCount;
        record.Type = agent.Type;
        record.Profiles = agent.Profiles ?? [];
        record.RoutingRules = agent.RoutingRules ?? [];
        record.Instruction = agent.Instruction ?? string.Empty;
        record.ChannelInstructions = agent.ChannelInstructions ?? [];
        record.Functions = agent.Functions ?? [];
        record.Templates = agent.Templates ?? [];
        record.Responses = agent.Responses ?? [];
        record.Samples = agent.Samples ?? [];
        record.Utilities = agent.Utilities ?? [];
        record.KnowledgeBases = agent.KnowledgeBases ?? [];
        if (agent.LlmConfig != null && !agent.LlmConfig.IsInherit)
        {
            record.LlmConfig = agent.LlmConfig;
        }

        _db.UpdateAgent(record, updateField);

        Utilities.ClearCache();
        await Task.CompletedTask;
    }

    public async Task<string> UpdateAgentFromFile(string id)
    {
        string updateResult;
        var dbSettings = _services.GetRequiredService<BotSharpDatabaseSettings>();
        var agentSettings = _services.GetRequiredService<AgentSettings>();

        if (dbSettings.Default == RepositoryEnum.FileRepository)
        {
            updateResult = $"Invalid database repository setting: {dbSettings.Default}";
            _logger.LogWarning(updateResult);
            return updateResult;
        }

        var agent = _db.GetAgent(id);
        if (agent == null)
        {
            updateResult = $"Cannot find agent ${id}";
            _logger.LogError(updateResult);
            return updateResult;
        }

        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                    dbSettings.FileRepository,
                                    agentSettings.DataDir);

        var clonedAgent = Agent.Clone(agent);
        var foundAgent = GetAgentFileById(agent.Id, filePath);
        if (foundAgent == null)
        {
            updateResult = $"Cannot find agent {agent.Name} in file directory: {filePath}";
            _logger.LogError(updateResult);
            return updateResult;
        }

        try
        {
            clonedAgent.SetId(foundAgent.Id)
                       .SetName(foundAgent.Name)
                       .SetDescription(foundAgent.Description)
                       .SetIsPublic(foundAgent.IsPublic)
                       .SetDisabled(foundAgent.Disabled)
                       .SetMergeUtility(foundAgent.MergeUtility)
                       .SetAgentType(foundAgent.Type)
                       .SetProfiles(foundAgent.Profiles)
                       .SetRoutingRules(foundAgent.RoutingRules)
                       .SetInstruction(foundAgent.Instruction)
                       .SetChannelInstructions(foundAgent.ChannelInstructions)
                       .SetTemplates(foundAgent.Templates)
                       .SetFunctions(foundAgent.Functions)
                       .SetResponses(foundAgent.Responses)
                       .SetSamples(foundAgent.Samples)
                       .SetUtilities(foundAgent.Utilities)
                       .SetLlmConfig(foundAgent.LlmConfig);

            _db.UpdateAgent(clonedAgent, AgentField.All);
            Utilities.ClearCache();

            updateResult = $"Agent {agent.Name} has been migrated!";
            _logger.LogInformation(updateResult);
            return updateResult;
        }
        catch (Exception ex)
        {
            updateResult = $"Failed to migrate agent {agent.Name} in file directory {filePath}.\r\nError: {ex.Message}";
            _logger.LogError(updateResult);
            return updateResult;
        }
    }


    public async Task<string> PatchAgentTemplate(Agent agent)
    {
        var patchResult = string.Empty;
        if (agent == null || agent.Templates.IsNullOrEmpty())
        {
            patchResult = $"Null agent instance or empty input templates";
            _logger.LogWarning(patchResult);
            return patchResult;
        }

        var record = _db.GetAgent(agent.Id);
        if (record == null)
        {
            patchResult = $"Cannot find agent {agent.Id}";
            _logger.LogWarning(patchResult);
            return patchResult;
        }

        var successTemplates = new List<string>();
        var failTemplates = new List<string>();
        foreach (var template in agent.Templates)
        {
            if (template == null) continue;

            var result = _db.PatchAgentTemplate(agent.Id, template);
            if (result)
            {
                successTemplates.Add(template.Name);
                _logger.LogInformation($"Template {template.Name} is updated successfully!");
            }
            else
            {
                failTemplates.Add(template.Name);
                _logger.LogWarning($"Template {template.Name} is failed to be updated!");
            }
        }

        Utilities.ClearCache();

        if (!successTemplates.IsNullOrEmpty())
        {
            patchResult += $"Success templates:\n{string.Join('\n', successTemplates)}\n\n";
        }

        if (!failTemplates.IsNullOrEmpty())
        {
            patchResult += $"Failed templates:\n{string.Join('\n', failTemplates)}";
        }

        return patchResult;
    }

    private Agent? GetAgentFileById(string agentId, string filePath)
    {
        if (!Directory.Exists(filePath)) return null;

        foreach (var dir in Directory.GetDirectories(filePath))
        {
            var agentJson = File.ReadAllText(Path.Combine(dir, "agent.json"));
            var agent = JsonSerializer.Deserialize<Agent>(agentJson, _options);
            if (agent != null && agent.Id == agentId)
            {
                var (defaultInstruction, channelInstructions) = GetInstructionsFromFile(dir);
                var functions = GetFunctionsFromFile(dir);
                var responses = GetResponsesFromFile(dir);
                var templates = GetTemplatesFromFile(dir);
                var samples = GetSamplesFromFile(dir);
                return agent.SetInstruction(defaultInstruction)
                            .SetChannelInstructions(channelInstructions)
                            .SetTemplates(templates)
                            .SetFunctions(functions)
                            .SetResponses(responses)
                            .SetSamples(samples);
            }
        }

        return null;
    }
}
