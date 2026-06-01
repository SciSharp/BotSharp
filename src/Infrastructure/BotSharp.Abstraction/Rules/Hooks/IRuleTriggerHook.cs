using BotSharp.Abstraction.Graph;
using BotSharp.Abstraction.Hooks;
using BotSharp.Abstraction.Rules.Models;

namespace BotSharp.Abstraction.Rules.Hooks;

public interface IRuleTriggerHook : IHookBase
{
    Task BeforeRuleConditionExecuting(Agent agent, FlowNode conditionNode, FlowEdge incomingEdge, IRuleTrigger trigger, RuleFlowContext context) => Task.CompletedTask;
    Task AfterRuleConditionExecuted(Agent agent, FlowNode conditionNode, FlowEdge incomingEdge, IRuleTrigger trigger, RuleFlowContext context, RuleNodeResult result) => Task.CompletedTask;

    Task BeforeRuleActionExecuting(Agent agent, FlowNode actionNode, FlowEdge incomingEdge, IRuleTrigger trigger, RuleFlowContext context) => Task.CompletedTask;
    Task AfterRuleActionExecuted(Agent agent, FlowNode actionNode, FlowEdge incomingEdge, IRuleTrigger trigger, RuleFlowContext context, RuleNodeResult result) => Task.CompletedTask;
}
