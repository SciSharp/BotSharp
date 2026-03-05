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

    [HttpGet("/rule/config/options")]
    public async Task<IDictionary<string, JsonDocument>> GetRuleConfigOptions()
    {
        var dict = new Dictionary<string, JsonDocument>();
        var configs = _services.GetServices<IRuleConfig>();

        foreach (var config in configs)
        {
            var json = await config.GetConfigAsync();
            dict[config.Provider.ToLower()] = json;
        }

        return dict;
    }
}
