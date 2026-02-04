using BotSharp.Abstraction.Hooks;
using BotSharp.Abstraction.Rules.Models;

namespace BotSharp.Abstraction.Rules.Hooks;

public interface IRuleTriggerHook : IHookBase
{
    Task BeforeRuleCriteriaExecuted(Agent agent, IRuleTrigger trigger, RuleCriteriaContext context) => Task.CompletedTask;
    Task AfterRuleCriteriaExecuted(Agent agent, IRuleTrigger trigger, RuleCriteriaResult result) => Task.CompletedTask;

    Task BeforeRuleActionExecuted(Agent agent, IRuleTrigger trigger, RuleActionContext context) => Task.CompletedTask;
    Task AfterRuleActionExecuted(Agent agent, IRuleTrigger trigger, RuleActionResult result) => Task.CompletedTask;
}
