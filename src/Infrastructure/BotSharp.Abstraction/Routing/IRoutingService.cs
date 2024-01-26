using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.Abstraction.Routing;

public interface IRoutingService
{
    Agent Router { get; }

    /// <summary>
    /// Get routable agents
    /// </summary>
    /// <param name="profiles">router's profile</param>
    /// <returns></returns>
    RoutableAgent[] GetRoutableAgents(List<string> profiles);

    RoutingRule[] GetRulesByName(string name);
    RoutingRule[] GetRulesByAgentId(string id);
    List<RoutingHandlerDef> GetHandlers();
    void ResetRecursiveCounter();
    Task<bool> InvokeAgent(string agentId, List<RoleDialogModel> dialogs);
    Task<bool> InvokeFunction(string name, RoleDialogModel message);
    Task<RoleDialogModel> InstructLoop(RoleDialogModel message);

    /// <summary>
    /// Talk to a specific Agent directly, bypassing the Router
    /// </summary>
    /// <param name="agent"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    Task<RoleDialogModel> InstructDirect(Agent agent, RoleDialogModel message);
}
