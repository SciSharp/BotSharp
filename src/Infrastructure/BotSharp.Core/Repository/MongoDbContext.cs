using BotSharp.Core.Repository.Collections;
using EntityFrameworkCore.BootKit;
using MongoDB.Driver;

namespace BotSharp.Core.Repository;

public class MongoDbContext : Database
{
    public IMongoCollection<Conversation> Conversations
        => Collection<Conversation>("conversations");
}
