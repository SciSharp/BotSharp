using BotSharp.Core.Rules.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotSharp.Core.Rules.Controllers;

[Authorize]
[ApiController]
public class RuleController : ControllerBase
{
    private readonly IServiceProvider _services;
    private readonly ILogger<RuleController> _logger;
    private readonly IRuleEngine _ruleEngine;

    public RuleController(
        IServiceProvider services,
        ILogger<RuleController> logger,
        IRuleEngine ruleEngine)
    {
        _services = services;
        _logger = logger;
        _ruleEngine = ruleEngine;
    }

    [HttpPost("/rule/trigger/action")]
    public async Task<IActionResult> RunAction([FromBody] RuleTriggerActionRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { Success = false, Error = "Request cannnot be empty." });
        }

        var trigger = _services.GetServices<IRuleTrigger>().FirstOrDefault(x => x.Name.IsEqualTo(request.TriggerName));
        if (trigger == null)
        {
            return BadRequest(new { Success = false, Error = "Unable to find rule trigger." });
        }

        var result = await _ruleEngine.Triggered(trigger, request.Text, request.States, request.Options);
        return Ok(new { Success = true });
    }
}
