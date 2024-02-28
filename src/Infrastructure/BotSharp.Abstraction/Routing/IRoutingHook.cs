using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Abstraction.Routing;

public interface IRoutingHook
{
    /// <summary>
    /// Routing instruction is received from Router
    /// </summary>
    /// <param name="instruct">routing instruction</param>
    /// <param name="message">message</param>
    /// <returns></returns>
    Task OnRoutingInstructionReceived(FunctionCallFromLlm instruct, RoleDialogModel message);

    Task OnAgentEnqueued(string agentId, string preAgentId, string? reason = null);

    Task OnAgentDequeued(string agentId, string currentAgentId, string? reason = null);

    Task OnAgentReplaced(string fromAgentId, string toAgentId, string? reason = null);

    Task OnAgentQueueEmptied(string agentId, string? reason = null);
}
