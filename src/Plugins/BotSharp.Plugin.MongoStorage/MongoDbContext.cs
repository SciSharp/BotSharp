using BotSharp.Plugin.MongoStorage.Collections;

namespace BotSharp.Plugin.MongoStorage;

public class MongoDbContext
{
    private readonly MongoClient _mongoClient;
    private readonly string _mongoDbDatabaseName;
    private readonly string _collectionPrefix;

    public MongoDbContext(BotSharpDatabaseSettings dbSettings)
    {
        var mongoDbConnectionString = dbSettings.BotSharpMongoDb;
        _mongoClient = new MongoClient(mongoDbConnectionString);
        _mongoDbDatabaseName = GetDatabaseName(mongoDbConnectionString);
        _collectionPrefix = dbSettings.TablePrefix.IfNullOrEmptyAs("BotSharp");
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
        => Database.GetCollection<AgentCollection>($"{_collectionPrefix}_Agents");

    public IMongoCollection<ConversationCollection> Conversations
        => Database.GetCollection<ConversationCollection>($"{_collectionPrefix}_Conversations");

    public IMongoCollection<ConversationDialogCollection> ConversationDialogs
        => Database.GetCollection<ConversationDialogCollection>($"{_collectionPrefix}_ConversationDialogs");

    public IMongoCollection<UserCollection> Users
        => Database.GetCollection<UserCollection>($"{_collectionPrefix}_Users");

    public IMongoCollection<UserAgentCollection> UserAgents
        => Database.GetCollection<UserAgentCollection>($"{_collectionPrefix}_UserAgents");
}
