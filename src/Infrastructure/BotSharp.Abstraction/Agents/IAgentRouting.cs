using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.Abstraction.Agents;

public interface IAgentRouting
{
    string AgentId { get; }
    Task<Agent> LoadRouter();
    RoutingRule[] GetRulesByName(string name);
}
