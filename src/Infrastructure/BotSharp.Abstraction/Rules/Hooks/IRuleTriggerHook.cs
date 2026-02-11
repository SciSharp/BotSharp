using BotSharp.Abstraction.Hooks;
using BotSharp.Abstraction.Rules.Models;

namespace BotSharp.Abstraction.Rules.Hooks;

public interface IRuleTriggerHook : IHookBase
{
    Task BeforeRuleCriteriaExecuted(Agent agent, AgentRuleCriteria ruleCriteria, IRuleTrigger trigger, RuleCriteriaContext context) => Task.CompletedTask;
    Task AfterRuleCriteriaExecuted(Agent agent, AgentRuleCriteria ruleCriteria, IRuleTrigger trigger, RuleCriteriaResult result) => Task.CompletedTask;

    Task BeforeRuleActionExecuted(Agent agent, AgentRuleAction ruleAction, IRuleTrigger trigger, RuleActionContext context) => Task.CompletedTask;
    Task AfterRuleActionExecuted(Agent agent, AgentRuleAction ruleAction, IRuleTrigger trigger, RuleActionResult result) => Task.CompletedTask;
}
