using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Rules;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class RulesController
{
    private readonly IServiceProvider _services;

    public RulesController(
        IServiceProvider services)
    {
        _services = services;
    }

    [HttpGet("/rule/triggers")]
    public IEnumerable<AgentRule> GetRuleTriggers()
    {
        var triggers = _services.GetServices<IRuleTrigger>();
        return triggers.Select(x => new AgentRule
        {
            TriggerName = x.GetType().Name
        }).OrderBy(x => x.TriggerName).ToList();
    }

    [HttpGet("/rule/formalization")]
    public async Task<string> GetFormalizedRuleDefinition([FromBody] AgentRule rule)
    {
        return "{}";
    }
}
