using BotSharp.Core.Repository.Collections;
using MongoDB.Driver;

namespace BotSharp.Core.Repository;

public class MongoDbContext : Database
{
    public IMongoCollection<ConversationCollection> Conversations
        => Collection<ConversationCollection>("conversations");
}
