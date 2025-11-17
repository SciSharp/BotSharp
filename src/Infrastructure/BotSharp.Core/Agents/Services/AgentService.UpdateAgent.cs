using BotSharp.Abstraction.Users.Enums;
using BotSharp.Abstraction.Users.Models;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task UpdateAgent(Agent agent, AgentField updateField)
    {
        if (agent == null || string.IsNullOrEmpty(agent.Id)) return;

        var userService = _services.GetRequiredService<IUserService>();
        var auth = await userService.GetUserAuthorizations([agent.Id]);
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
        record.Mode = agent.Mode;
        record.FuncVisMode = agent.FuncVisMode;
        record.Profiles = agent.Profiles ?? [];
        record.Labels = agent.Labels ?? [];
        record.RoutingRules = agent.RoutingRules ?? [];
        record.Instruction = agent.Instruction ?? string.Empty;
        record.ChannelInstructions = agent.ChannelInstructions ?? [];
        record.Functions = agent.Functions ?? [];
        record.Templates = agent.Templates ?? [];
        record.Responses = agent.Responses ?? [];
        record.Samples = agent.Samples ?? [];
        record.Utilities = agent.Utilities ?? [];
        record.McpTools = agent.McpTools ?? [];
        record.KnowledgeBases = agent.KnowledgeBases ?? [];
        record.Rules = agent.Rules ?? [];
        if (agent.LlmConfig != null && !agent.LlmConfig.IsInherit)
        {
            record.LlmConfig = agent.LlmConfig;
        }

        _db.UpdateAgent(record, updateField);

        Utilities.ClearCache();
        await Task.CompletedTask;
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
}
