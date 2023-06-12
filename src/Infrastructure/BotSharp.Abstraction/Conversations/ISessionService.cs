using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Abstraction.Conversations;

public interface ISessionService
{
    Task<Session> NewSession(Session sess);
    Task<List<Session>> GetSessions();
    Task DeleteSession(string sessionId);
}
