using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Core.Repository;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Core.Conversations;

public class SessionService : ISessionService
{
    private readonly IServiceProvider _services;

    public SessionService(IServiceProvider services)
    {
        _services = services;
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
        var mongo = _services.CreateScope().ServiceProvider.GetRequiredService<MongoDbContext>();

        return new SessionModel
        {
            SessionId = Guid.NewGuid().ToString()
        };
    }
}
