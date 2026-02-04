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
            TriggerName = x.Name,
            Channel = x.Channel,
            Statement = x.Statement,
            OutputArgs = x.OutputArgs
        }).OrderBy(x => x.TriggerName);
    }

    [HttpGet("/rule/criteria-providers")]
    public async Task<IEnumerable<string>> GetRuleCriteriaProviders()
    {
        return _services.GetServices<IRuleCriteria>().Select(x => x.Provider).OrderBy(x => x);
    }

    [HttpGet("/rule/actions")]
    public async Task<IEnumerable<string>> GetRuleActions()
    {
        return _services.GetServices<IRuleAction>().Select(x => x.Name).OrderBy(x => x);
    }
}
