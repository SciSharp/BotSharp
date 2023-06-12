using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Users;

namespace BotSharp.Core.Conversations.Services;

public class SessionService : ISessionService
{
    private readonly IServiceProvider _services;
    private readonly ICurrentUser _user;

    public SessionService(IServiceProvider services, ICurrentUser user)
    {
        _services = services;
        _user = user;
    }

    public Task DeleteSession(string sessionId)
    {
        throw new NotImplementedException();
    }

    public async Task<List<Session>> GetSessions()
    {
        var db = _services.GetRequiredService<AgentDbContext>();
        var query = from sess in db.Session
                    where sess.UserId == _user.Id
                    orderby sess.CreatedTime descending
                    select sess.ToSession();
        return query.ToList();
    }

    public async Task<Session> NewSession(Session sess)
    {
        var db = _services.GetRequiredService<AgentDbContext>();

        var record = SessionRecord.FromSession(sess);
        record.Id = Guid.NewGuid().ToString();
        record.UserId = _user.Id;
        record.Title = "New Session";

        db.Transaction<IAgentTable>(delegate
        {
            db.Add<IAgentTable>(record);
        });

        return record.ToSession();
    }
}
