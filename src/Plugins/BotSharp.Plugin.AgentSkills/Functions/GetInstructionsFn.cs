using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Routing;
using BotSharp.Plugin.AgentSkills.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Plugin.AgentSkills.Functions;

/// <summary>
/// Get a list of the available skills
/// </summary>
public class GetInstructionsFn : IFunctionCallback
{
    private readonly ISkillService _skillService;
    private readonly IAgentService _agentService;
    private readonly IRoutingContext _routingCtx;


    public string Name => "get-available-skills";

    public string Provider => "AgentSkills";


    public GetInstructionsFn(ISkillService skillService , IAgentService agentService,
        IRoutingContext routingCtx)
    {
        _skillService = skillService;
        _agentService = agentService;
        _routingCtx = routingCtx;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var agentId = _routingCtx.GetCurrentAgentId();
        var agent = await _agentService.GetAgent(agentId);

        var skills = _skillService.GetAgentSkills(agent);
        if(skills != null && skills.Count == 0)
        {
            message.Content = "<available_skills></available_skills>";
            return true;
        }
        StringBuilder availableSkillToolBuilder = new();
        availableSkillToolBuilder.AppendLine("<available_skills>");
        foreach (Skills.AgentSkill skill in skills)
        {
            availableSkillToolBuilder.AppendLine("\t<skill>");
            availableSkillToolBuilder.AppendLine($"\t\t<name>{skill.Name}</name>");
            availableSkillToolBuilder.AppendLine($"\t\t<description>{skill.Description}</description>");
            availableSkillToolBuilder.AppendLine($"\t\t<location>{skill.FolderPath}</location>");
            availableSkillToolBuilder.AppendLine("\t</skill>");
        }

        availableSkillToolBuilder.AppendLine("</available_skills>");
        message.Content = availableSkillToolBuilder.ToString();
        return true;
    }
}
