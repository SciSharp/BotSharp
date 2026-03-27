using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Routing;
using BotSharp.Plugin.AgentSkills.Services;
using BotSharp.Plugin.AgentSkills.Skills;
using System.Text.Json;
using System.Threading.Tasks;

namespace BotSharp.Plugin.AgentSkills.Functions;

public class GetSkillBynameFn : IFunctionCallback
{
    private readonly ISkillService _skillService;
    private readonly AgentSkillsSettings _settings;
    private readonly IAgentService _agentService;
    private readonly IRoutingContext _routingCtx;

    public string Name => "get-skill-by-name";

    public string Provider => "AgentSkills";

    public GetSkillBynameFn(ISkillService skillService, AgentSkillsSettings settings, IAgentService agentService,
        IRoutingContext routingCtx)
    {
        _skillService = skillService;
        _agentService = agentService;
        _routingCtx = routingCtx;
        _settings = settings;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var agentId = _routingCtx.GetCurrentAgentId();
        var agent = await _agentService.GetAgent(agentId);
        var Skills = _skillService.GetAgentSkills(agent); 
        var args = JsonSerializer.Deserialize<GetSkillArgs>(message.FunctionArgs) ?? new();
        var options = new AgentSkillsAsToolsOptions
        {
            IncludeToolForFileContentRead = _settings.EnableReadFileTool
        };

        Skills.AgentSkill? skill = Skills.FirstOrDefault(x => x.Name.Equals(args.SkillName, StringComparison.CurrentCultureIgnoreCase));
        var contents = skill != null ? skill.GenerateDefinition(options.AgentSkillAsToolOptions) : $"Error: Skill with name '{args.SkillName}' was not found";
        message.Content = contents;
        return true;
    }
}
