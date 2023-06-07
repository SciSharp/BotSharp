using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Core.Repository;
using BotSharp.Core.Repository.Collections;
using EntityFrameworkCore.BootKit;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

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

    public async Task<SessionModel> NewSession(string userId)
    {
        var mongo = _services.CreateScope().ServiceProvider.GetRequiredService<MongoDbContext>();

        var record = new Conversation
        {
            CreatedAt = DateTime.UtcNow,
            Messages = new List<MessageModel>(),
            UserId = "anonymous",
            Model = "OpenAssistant/oasst-sft-4-pythia-12b-epoch-3.5"
        };
        await mongo.Conversations.InsertOneAsync(record);

        return new SessionModel
        {
            SessionId = record.Id.ToString(),
            UserId = record.UserId
        };
    }
}
