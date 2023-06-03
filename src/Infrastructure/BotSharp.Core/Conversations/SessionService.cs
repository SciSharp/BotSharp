using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Core.Conversations;

public class SessionService : ISessionService
{
    Dictionary<string, List<string>> _sessions;

    public SessionService()
    {
        _sessions = new Dictionary<string, List<string>>();
    }

    public void DeleteSession(string sessionId)
    {
        throw new NotImplementedException();
    }

    public List<string> GetAllSessions(string userId)
    {
        throw new NotImplementedException();
    }

    public SessionModel NewSession(string userId)
    {
        return new SessionModel
        {
            SessionId = Guid.NewGuid().ToString()
        };
    }
}
