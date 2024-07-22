using System.Web;

namespace BotSharp.Plugin.MongoStorage;

public class MongoDbContext
{
    private readonly MongoClient _mongoClient;
    private readonly string _mongoDbDatabaseName;
    private readonly string _collectionPrefix;

    private readonly IEnumerable<string> _dbKeys = new List<string>()
    {
        "authSource"
    };

    public MongoDbContext(BotSharpDatabaseSettings dbSettings)
    {
        var mongoDbConnectionString = dbSettings.BotSharpMongoDb;
        _mongoClient = new MongoClient(mongoDbConnectionString);
        _mongoDbDatabaseName = GetDatabaseName(mongoDbConnectionString);
        _collectionPrefix = dbSettings.TablePrefix.IfNullOrEmptyAs("BotSharp");
    }

    private string GetDatabaseName(string mongoDbConnectionString)
    {
        var dbName = string.Empty;
        if (!Uri.TryCreate(mongoDbConnectionString, UriKind.Absolute, out var conn))
        {
            return dbName;
        }

        var query = HttpUtility.ParseQueryString(conn.Query);
        dbName = conn.Segments?.FirstOrDefault(x => x != "/") ?? string.Empty;
        var keys = query.AllKeys ?? [];
        foreach (var db in _dbKeys)
        {
            if (keys.Contains(db))
            {
                dbName = query[db];
                break;
            }
        }
        return dbName;
    }


    private IMongoDatabase Database { get { return _mongoClient.GetDatabase(_mongoDbDatabaseName); } }

    #region Indexes
    private IMongoCollection<ConversationDocument> CreateConversationIndex()
    {
        var collection = Database.GetCollection<ConversationDocument>($"{_collectionPrefix}_Conversations");
        var indexes = collection.Indexes.List().ToList();
        var createTimeIndex = indexes.FirstOrDefault(x => x.GetElement("name").ToString().StartsWith("CreatedTime"));
        if (createTimeIndex == null)
        {
            var indexDef = Builders<ConversationDocument>.IndexKeys.Descending(x => x.CreatedTime);
            collection.Indexes.CreateOne(new CreateIndexModel<ConversationDocument>(indexDef));
        }
        return collection;
    }

    private IMongoCollection<ConversationStateDocument> CreateConversationStateIndex()
    {
        var collection = Database.GetCollection<ConversationStateDocument>($"{_collectionPrefix}_ConversationStates");
        var indexes = collection.Indexes.List().ToList();
        var stateIndex = indexes.FirstOrDefault(x => x.GetElement("name").ToString().StartsWith("States.Key"));
        if (stateIndex == null)
        {
            var indexDef = Builders<ConversationStateDocument>.IndexKeys.Ascending("States.Key");
            collection.Indexes.CreateOne(new CreateIndexModel<ConversationStateDocument>(indexDef));
        }
        return collection;
    }

    private IMongoCollection<AgentTaskDocument> CreateAgentTaskIndex()
    {
        var collection = Database.GetCollection<AgentTaskDocument>($"{_collectionPrefix}_AgentTasks");
        var indexes = collection.Indexes.List().ToList();
        var createTimeIndex = indexes.FirstOrDefault(x => x.GetElement("name").ToString().StartsWith("CreatedTime"));
        if (createTimeIndex == null)
        {
            var indexDef = Builders<AgentTaskDocument>.IndexKeys.Descending(x => x.CreatedTime);
            collection.Indexes.CreateOne(new CreateIndexModel<AgentTaskDocument>(indexDef));
        }
        return collection;
    }

    private IMongoCollection<ConversationContentLogDocument> CreateContentLogIndex()
    {
        var collection = Database.GetCollection<ConversationContentLogDocument>($"{_collectionPrefix}_ConversationContentLogs");
        var indexes = collection.Indexes.List().ToList();
        var createTimeIndex = indexes.FirstOrDefault(x => x.GetElement("name").ToString().StartsWith("CreateTime"));
        if (createTimeIndex == null)
        {
            var indexDef = Builders<ConversationContentLogDocument>.IndexKeys.Ascending(x => x.CreateTime);
            collection.Indexes.CreateOne(new CreateIndexModel<ConversationContentLogDocument>(indexDef));
        }
        return collection;
    }

    private IMongoCollection<ConversationStateLogDocument> CreateStateLogIndex()
    {
        var collection = Database.GetCollection<ConversationStateLogDocument>($"{_collectionPrefix}_ConversationStateLogs");
        var indexes = collection.Indexes.List().ToList();
        var createTimeIndex = indexes.FirstOrDefault(x => x.GetElement("name").ToString().StartsWith("CreateTime"));
        if (createTimeIndex == null)
        {
            var indexDef = Builders<ConversationStateLogDocument>.IndexKeys.Ascending(x => x.CreateTime);
            collection.Indexes.CreateOne(new CreateIndexModel<ConversationStateLogDocument>(indexDef));
        }
        return collection;
    }
    #endregion

    public IMongoCollection<AgentDocument> Agents
        => Database.GetCollection<AgentDocument>($"{_collectionPrefix}_Agents");

    public IMongoCollection<AgentTaskDocument> AgentTasks
        => CreateAgentTaskIndex();

    public IMongoCollection<ConversationDocument> Conversations
        => CreateConversationIndex();

    public IMongoCollection<ConversationDialogDocument> ConversationDialogs
        => Database.GetCollection<ConversationDialogDocument>($"{_collectionPrefix}_ConversationDialogs");

    public IMongoCollection<ConversationStateDocument> ConversationStates
        => CreateConversationStateIndex();

    public IMongoCollection<ExecutionLogDocument> ExectionLogs
        => Database.GetCollection<ExecutionLogDocument>($"{_collectionPrefix}_ExecutionLogs");

    public IMongoCollection<LlmCompletionLogDocument> LlmCompletionLogs
        => Database.GetCollection<LlmCompletionLogDocument>($"{_collectionPrefix}_LlmCompletionLogs");

    public IMongoCollection<ConversationContentLogDocument> ContentLogs
        => CreateContentLogIndex();

    public IMongoCollection<ConversationStateLogDocument> StateLogs
        => CreateStateLogIndex();

    public IMongoCollection<UserDocument> Users
        => Database.GetCollection<UserDocument>($"{_collectionPrefix}_Users");

    public IMongoCollection<UserAgentDocument> UserAgents
        => Database.GetCollection<UserAgentDocument>($"{_collectionPrefix}_UserAgents");

    public IMongoCollection<PluginDocument> Plugins
        => Database.GetCollection<PluginDocument>($"{_collectionPrefix}_Plugins");

    public IMongoCollection<TranslationMemoryDocument> TranslationMemories
        => Database.GetCollection<TranslationMemoryDocument>($"{_collectionPrefix}_TranslationMemories");
}
