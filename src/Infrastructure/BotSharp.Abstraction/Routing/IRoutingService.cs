using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.Abstraction.Routing;

public interface IRoutingService
{
    Agent Router { get; }
    RoutingItem[] GetRoutingItems();
    RoutingRule[] GetRulesByName(string name);
    RoutingRule[] GetRulesByAgentId(string id);
    List<RoutingHandlerDef> GetHandlers();
    void ResetRecursiveCounter();
    Task<bool> InvokeAgent(string agentId, List<RoleDialogModel> dialogs);
    Task<RoleDialogModel> InstructLoop(RoleDialogModel message);

    /// <summary>
    /// Talk to a specific Agent directly, bypassing the Router
    /// </summary>
    /// <param name="agent"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    Task<RoleDialogModel> ExecuteDirectly(Agent agent, RoleDialogModel message);
}
