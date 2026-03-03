using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Hooks;
using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.Abstraction.Routing;

public interface IRoutingHook : IHookBase
{
    /// <summary>
    /// Routing instruction is received from Router
    /// </summary>
    /// <param name="instruct">routing instruction</param>
    /// <param name="message">message</param>
    /// <returns></returns>
    Task OnRoutingInstructionReceived(FunctionCallFromLlm instruct, RoleDialogModel message)
        => Task.CompletedTask;

    Task OnRoutingInstructionRevised(FunctionCallFromLlm instruct, RoleDialogModel message)
        => Task.CompletedTask;

    Task OnAgentEnqueued(string agentId, string preAgentId, string? reason = null)
        => Task.CompletedTask;

    Task OnAgentDequeued(string agentId, string currentAgentId, string? reason = null)
        => Task.CompletedTask;

    Task OnAgentReplaced(string fromAgentId, string toAgentId, string? reason = null)
        => Task.CompletedTask;

    Task OnAgentQueueEmptied(string agentId, string? reason = null)
        => Task.CompletedTask;

    /// <summary>
    /// Called when routing rules are loaded for an agent (GetRulesByAgentName / GetRulesByAgentId).
    /// Hooks can modify rules in place to rewrite or filter rules before they are returned.
    /// </summary>
    /// <param name="agentId">Agent id the rules belong to.</param>
    /// <param name="rules">Mutable list of routing rules.</param>
    Task OnRoutingRulesLoaded(string agentId, IList<RoutingRule> rules)
        => Task.CompletedTask;
}
