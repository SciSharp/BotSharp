using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Abstraction.Conversations;

public interface ISessionService
{
    Task<SessionModel> NewSession(string userId);
    List<string> GetAllSessions(string userId);
    void DeleteSession(string sessionId);
}
