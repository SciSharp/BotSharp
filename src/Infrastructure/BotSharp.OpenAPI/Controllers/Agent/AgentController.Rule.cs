using BotSharp.Abstraction.Rules;
using BotSharp.Abstraction.Rules.Models;

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
    public async Task<IDictionary<string, RuleConfigModel>> GetRuleConfigOptions()
    {
        var dict = new Dictionary<string, RuleConfigModel>();
        var flows = _services.GetServices<IRuleFlow<RuleGraph>>();

        foreach (var flow in flows)
        {
            var config = await flow.GetTopologyConfigAsync();
            dict[config.TopologyProvider.ToLower()] = config;
        }

        return dict;
    }
}
