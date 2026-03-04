using BotSharp.Abstraction.Hooks;
using BotSharp.Abstraction.Rules.Models;

namespace BotSharp.Abstraction.Rules.Hooks;

public interface IRuleTriggerHook : IHookBase
{
    Task BeforeRuleConditionExecuted(Agent agent, RuleNode conditionNode, IRuleTrigger trigger, RuleFlowContext context) => Task.CompletedTask;
    Task AfterRuleConditionExecuted(Agent agent, RuleNode conditionNode, IRuleTrigger trigger, RuleNodeResult result) => Task.CompletedTask;

    Task BeforeRuleActionExecuted(Agent agent, RuleNode actionNode, IRuleTrigger trigger, RuleFlowContext context) => Task.CompletedTask;
    Task AfterRuleActionExecuted(Agent agent, RuleNode actionNode, IRuleTrigger trigger, RuleNodeResult result) => Task.CompletedTask;
}
