namespace BotSharp.Abstraction.Agents;

public interface IAgentRouting
{
    Task<Agent> LoadRouter();
    Task<Agent> LoadCurrentAgent();
    RoutingRecord[] GetRoutingRecords();
}
