using BotSharp.Abstraction.Rules;
using BotSharp.Abstraction.Rules.Settings;

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

    [HttpGet("/rule/config/options")]
    public IDictionary<string, IEnumerable<string>> GetRuleConfigOptions()
    {
        var settings = _services.GetRequiredService<RuleSettings>();
        var options = settings?.ConfigOptions ?? [];
        return options;
    }
}
