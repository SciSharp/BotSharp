using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Routing;
using BotSharp.Plugin.AgentSkills.Services;
using BotSharp.Plugin.AgentSkills.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BotSharp.Plugin.AgentSkills.Functions;

/// <summary>
/// Read the content of a Skill File by its path
/// </summary>
public class GetSkillFileContentFn : IFunctionCallback
{
    private readonly ISkillService _skillService;
    private readonly IAgentService _agentService;
    private readonly IRoutingContext _routingCtx;

    public string Name => "read-skill-file-content";

    public string Provider => "AgentSkills";

    public GetSkillFileContentFn(ISkillService skillService, IAgentService agentService,
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
        var Skills = _skillService.GetAgentSkills(agent);

        IEnumerable<string> allowedFiles = Skills.SelectMany(x => x.AssetFiles.Union(x.OtherFiles).Union(x.ScriptFiles).Union(x.ReferenceFiles));

        var args = JsonSerializer.Deserialize<SkillFileContentArgs>(message.FunctionArgs) ?? new();

        var filePath = args.FilePath;
        var contents = string.Empty;
        if (!allowedFiles.Contains(filePath))
        {
             contents = $"Error: File '{filePath}' is not a valid Skill-file";
        }

        contents =  File.ReadAllText(args.FilePath, Encoding.UTF8);
        message.Content = contents; 
        return true;
    }
}
