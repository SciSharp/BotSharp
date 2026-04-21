using BotSharp.Plugin.AgentSkills.Services;
using BotSharp.Plugin.AgentSkills.Skills;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotSharp.Plugin.AgentSkills.Controllers;

[Authorize]
[ApiController]
public class SkillController : ControllerBase
{
    private readonly ISkillService _skillService;

    public SkillController(ISkillService skillService)
    {
        _skillService = skillService ?? throw new ArgumentNullException(nameof(skillService));
    }

    [HttpGet("/skills")]
    public IList<Skills.AgentSkill>  GetAgentSkills()
    {
        return _skillService.GetAgentSkills();
    }
}
