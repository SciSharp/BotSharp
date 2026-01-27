using BotSharp.Abstraction.Rules.Models;

namespace BotSharp.Abstraction.Rules.Hooks;

public class RuleTriggerHookBase : IRuleTriggerHook
{
    public string SelfId => string.Empty;

    public Task BeforeRuleActionExecuted(Agent agent, IRuleTrigger trigger, RuleActionContext context)
    {
        return Task.CompletedTask; 
    }

    public Task AfterRuleActionExecuted(Agent agent, IRuleTrigger trigger, RuleActionResult result)
    {
        return Task.CompletedTask;
    }
}
