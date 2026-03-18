using BotSharp.Abstraction.Hooks;
using BotSharp.Abstraction.Rules.Models;

namespace BotSharp.Abstraction.Rules.Hooks;

public interface IRuleTriggerHook : IHookBase
{
    Task BeforeRuleConditionExecuting(Agent agent, RuleNode conditionNode, RuleEdge incomingEdge, IRuleTrigger trigger, RuleFlowContext context) => Task.CompletedTask;
    Task AfterRuleConditionExecuted(Agent agent, RuleNode conditionNode, RuleEdge incomingEdge, IRuleTrigger trigger, RuleFlowContext context, RuleNodeResult result) => Task.CompletedTask;

    Task BeforeRuleActionExecuting(Agent agent, RuleNode actionNode, RuleEdge incomingEdge, IRuleTrigger trigger, RuleFlowContext context) => Task.CompletedTask;
    Task AfterRuleActionExecuted(Agent agent, RuleNode actionNode, RuleEdge incomingEdge, IRuleTrigger trigger, RuleFlowContext context, RuleNodeResult result) => Task.CompletedTask;
}
