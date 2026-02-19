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
    public async Task<IEnumerable<KeyValue>> GetRuleCriteriaProviders()
    {
        return _services.GetServices<IRuleCriteria>().OrderBy(x => x.Provider).Select(x => new KeyValue
        {
            Key = x.Provider,
            Value = x.DefaultConfig != null ? x.DefaultConfig.RootElement.GetRawText() : "{}"
        });
    }

    [HttpGet("/rule/actions")]
    public async Task<IEnumerable<KeyValue>> GetRuleActions()
    {
        return _services.GetServices<IRuleAction>().OrderBy(x => x.Name).Select(x => new KeyValue
        {
            Key = x.Name,
            Value = x.DefaultConfig != null ? x.DefaultConfig.RootElement.GetRawText() : "{}"
        });
    }
}
