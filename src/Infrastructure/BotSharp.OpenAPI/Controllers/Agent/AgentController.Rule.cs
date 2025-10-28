using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Rules;

namespace BotSharp.OpenAPI.Controllers;

public partial class AgentController
{
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
