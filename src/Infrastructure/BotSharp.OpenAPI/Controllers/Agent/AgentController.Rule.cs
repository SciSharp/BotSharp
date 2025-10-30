using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Rules;

namespace BotSharp.OpenAPI.Controllers;

public partial class AgentController
{
    [HttpGet("/rule/triggers")]
    public IEnumerable<AgentRuleViewModel> GetRuleTriggers()
    {
        var triggers = _services.GetServices<IRuleTrigger>();
        return triggers.Select(x => new AgentRuleViewModel
        {
            TriggerName = x.GetType().Name,
            OutputArgs = x.OutputArgs
        }).OrderBy(x => x.TriggerName).ToList();
    }

    [HttpGet("/rule/formalization")]
    public async Task<string> GetFormalizedRuleDefinition([FromBody] AgentRule rule)
    {
        return "{}";
    }
}
