namespace BotSharp.Abstraction.Agents;

public interface IAgentRouting
{
    Task<Agent> LoadCurrentAgent();
}
