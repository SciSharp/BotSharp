namespace BotSharp.Abstraction.Agents;

/// <summary>
/// Agent management service
/// </summary>
public interface IAgentService
{
    void NewAgent();
    void DeleteAgent();
    void UpdateAgent();
}
