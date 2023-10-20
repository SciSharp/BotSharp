using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.Abstraction.Routing;

public interface IRouterInstance
{
    string AgentId { get; }
    Agent Router { get; }
    RoutingItem[] GetRoutingItems();
    List<RoutingHandlerDef> GetHandlers();
    IRouterInstance Load();
    RoutingRule[] GetRulesByName(string name);
}
