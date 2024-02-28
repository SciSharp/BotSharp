using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Abstraction.Routing;

public interface IRoutingHook
{
    /// <summary>
    /// Conversation is redirected to another agent
    /// </summary>
    /// <param name="toAgentId"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    Task OnConversationRedirected(string toAgentId, RoleDialogModel message);

    /// <summary>
    /// Routing instruction is received from Router
    /// </summary>
    /// <param name="instruct">routing instruction</param>
    /// <param name="message">message</param>
    /// <returns></returns>
    Task OnConversationRouting(FunctionCallFromLlm instruct, RoleDialogModel message);

    Task OnAgentEnqueued(string agentId, string preAgentId);

    Task OnAgentDequeued(string agentId, string currentAgentId);

    Task OnAgentReplaced(string fromAgentId, string toAgentId);

    Task OnAgentQueueEmptied(string agentId);
}
