using BotSharp.Plugin.MongoStorage.Collections;

namespace BotSharp.Plugin.MongoStorage;

public class MongoDbContext
{
    private readonly MongoClient _mongoClient;
    private readonly string _mongoDbDatabaseName;

    public MongoDbContext(string mongoDbConnectionString)
    {
        _mongoClient = new MongoClient(mongoDbConnectionString);
        _mongoDbDatabaseName = GetDatabaseName(mongoDbConnectionString);
    }

    private string GetDatabaseName(string mongoDbConnectionString)
    {
        var databaseName = mongoDbConnectionString.Substring(mongoDbConnectionString.LastIndexOf("/", StringComparison.InvariantCultureIgnoreCase) + 1);
        if (databaseName.Contains("?"))
        {
            databaseName = databaseName.Substring(0, databaseName.IndexOf("?", StringComparison.InvariantCultureIgnoreCase));
        }
        return databaseName;
    }

    private IMongoDatabase Database { get { return _mongoClient.GetDatabase(_mongoDbDatabaseName); } }

    public IMongoCollection<AgentCollection> Agents
        => Database.GetCollection<AgentCollection>("OneBrainAgents");

    public IMongoCollection<ConversationCollection> Conversations
        => Database.GetCollection<ConversationCollection>("OneBrainConversations");

    public IMongoCollection<UserCollection> Users
        => Database.GetCollection<UserCollection>("OneBrainUsers");

    public IMongoCollection<UserAgentCollection> UserAgents
        => Database.GetCollection<UserAgentCollection>("OneBrainUserAgents");
}
