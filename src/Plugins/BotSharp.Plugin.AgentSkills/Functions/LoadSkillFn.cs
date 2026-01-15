using System.Threading.Tasks;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Conversations;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Text.Json;

namespace BotSharp.Plugin.AgentSkills.Functions;

public class LoadSkillFn : IFunctionCallback
{
    public string Name => "load_skill";
    public string Indication => "Loading skill...";
    private readonly IServiceProvider _services;

    public LoadSkillFn(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var skillName = message.FunctionName == "load_skill" 
            ? JsonSerializer.Deserialize<JsonElement>(message.FunctionArgs).GetProperty("skill_name").GetString()
            : null;

        if (string.IsNullOrEmpty(skillName))
        {
            message.Content = "Error: skill_name provided.";
            return false;
        }

        var stateService = _services.GetRequiredService<IConversationStateService>();
        var currentActiveStr = stateService.GetState("active_skills");
        var currentActive = string.IsNullOrEmpty(currentActiveStr) 
            ? new List<string>() 
            : currentActiveStr.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        if (!currentActive.Contains(skillName))
        {
            currentActive.Add(skillName);
            stateService.SetState("active_skills", string.Join(",", currentActive));
            message.Content = $"Skill '{skillName}' has been activated. The detailed instructions will be available in the next step.";
        }
        else
        {
            message.Content = $"Skill '{skillName}' is already active.";
        }

        return true;
    }
}
