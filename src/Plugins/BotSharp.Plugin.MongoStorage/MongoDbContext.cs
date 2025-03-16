namespace BotSharp.Plugin.MongoStorage;

public class MongoDbContext
{
    private readonly MongoClient _mongoClient;
    private readonly string _mongoDbDatabaseName;
    private readonly string _collectionPrefix;

    private const string DB_NAME_INDEX = "authSource";

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

        var symbol = "?";
        if (databaseName.Contains(symbol))
        {
            var markIdx = databaseName.IndexOf(symbol, StringComparison.InvariantCultureIgnoreCase);
            var db = databaseName.Substring(0, markIdx);
            if (!string.IsNullOrWhiteSpace(db))
            {
                return db;
            }

            var queryStr = databaseName.Substring(markIdx + 1);
            var queries = queryStr.Split("&", StringSplitOptions.RemoveEmptyEntries).Select(x => new
            {
                Key = x.Split("=")[0],
                Value = x.Split("=")[1]
            }).ToList();

            var source = queries.FirstOrDefault(x => x.Key.IsEqualTo(DB_NAME_INDEX));
            if (source != null)
            {
                databaseName = source.Value;
            }
        }
        return databaseName;
    }

    private IMongoDatabase Database => _mongoClient.GetDatabase(_mongoDbDatabaseName);

    #region Private methods
    private bool CollectionExists(IMongoDatabase database, string collectionName)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("name", collectionName);
        var collections = database.ListCollectionNames(new ListCollectionNamesOptions { Filter = filter }).ToList();
        return collections.Any();
    }

    private IMongoCollection<TDocument> GetCollectionOrCreate<TDocument>(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"The collection {name} cannot be empty.");

        var collectionName = $"{_collectionPrefix}_{name}";
        if (!CollectionExists(Database, collectionName))
        {
            Database.CreateCollection(collectionName);
        }

        var collection = Database.GetCollection<TDocument>(collectionName);
        return collection;
    }

    #region Indexes
    private IMongoCollection<ConversationDocument> CreateConversationIndex()
    {
        var collection = GetCollectionOrCreate<ConversationDocument>("Conversations");
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
        var collection = GetCollectionOrCreate<ConversationStateDocument>("ConversationStates");
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
        var collection = GetCollectionOrCreate<AgentTaskDocument>("AgentTasks");
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
        var collection = GetCollectionOrCreate<ConversationContentLogDocument>("ConversationContentLogs");
        var indexes = collection.Indexes.List().ToList();
        var createTimeIndex = indexes.FirstOrDefault(x => x.GetElement("name").ToString().StartsWith("CreatedTime"));
        if (createTimeIndex == null)
        {
            var indexDef = Builders<ConversationContentLogDocument>.IndexKeys.Ascending(x => x.CreatedTime);
            collection.Indexes.CreateOne(new CreateIndexModel<ConversationContentLogDocument>(indexDef));
        }
        return collection;
    }

    private IMongoCollection<ConversationStateLogDocument> CreateStateLogIndex()
    {
        var collection = GetCollectionOrCreate<ConversationStateLogDocument>("ConversationStateLogs");
        var indexes = collection.Indexes.List().ToList();
        var createTimeIndex = indexes.FirstOrDefault(x => x.GetElement("name").ToString().StartsWith("CreatedTime"));
        if (createTimeIndex == null)
        {
            var indexDef = Builders<ConversationStateLogDocument>.IndexKeys.Ascending(x => x.CreatedTime);
            collection.Indexes.CreateOne(new CreateIndexModel<ConversationStateLogDocument>(indexDef));
        }
        return collection;
    }

    private IMongoCollection<InstructionLogDocument> CreateInstructionLogIndex()
    {
        var collection = GetCollectionOrCreate<InstructionLogDocument>("InstructionLogs");
        var indexes = collection.Indexes.List().ToList();
        var createTimeIndex = indexes.FirstOrDefault(x => x.GetElement("name").ToString().StartsWith("CreatedTime"));
        if (createTimeIndex == null)
        {
            var indexDef = Builders<InstructionLogDocument>.IndexKeys.Descending(x => x.CreatedTime);
            collection.Indexes.CreateOne(new CreateIndexModel<InstructionLogDocument>(indexDef));
        }
        return collection;
    }
    #endregion
    #endregion

    public IMongoCollection<AgentDocument> Agents
        => GetCollectionOrCreate<AgentDocument>("Agents");

    public IMongoCollection<AgentTaskDocument> AgentTasks
        => CreateAgentTaskIndex();

    public IMongoCollection<ConversationDocument> Conversations
        => CreateConversationIndex();

    public IMongoCollection<ConversationDialogDocument> ConversationDialogs
        => GetCollectionOrCreate<ConversationDialogDocument>("ConversationDialogs");

    public IMongoCollection<ConversationStateDocument> ConversationStates
        => CreateConversationStateIndex();

    public IMongoCollection<LlmCompletionLogDocument> LlmCompletionLogs
        => GetCollectionOrCreate<LlmCompletionLogDocument>("LlmCompletionLogs");

    public IMongoCollection<ConversationContentLogDocument> ContentLogs
        => CreateContentLogIndex();

    public IMongoCollection<ConversationStateLogDocument> StateLogs
        => CreateStateLogIndex();

    public IMongoCollection<UserDocument> Users
        => GetCollectionOrCreate<UserDocument>("Users");

    public IMongoCollection<UserAgentDocument> UserAgents
        => GetCollectionOrCreate<UserAgentDocument>("UserAgents");

    public IMongoCollection<PluginDocument> Plugins
        => GetCollectionOrCreate<PluginDocument>("Plugins");

    public IMongoCollection<TranslationMemoryDocument> TranslationMemories
        => GetCollectionOrCreate<TranslationMemoryDocument>("TranslationMemories");

    public IMongoCollection<KnowledgeCollectionConfigDocument> KnowledgeCollectionConfigs
        => GetCollectionOrCreate<KnowledgeCollectionConfigDocument>("KnowledgeCollectionConfigs");

    public IMongoCollection<KnowledgeCollectionFileMetaDocument> KnowledgeCollectionFileMeta
        => GetCollectionOrCreate<KnowledgeCollectionFileMetaDocument>("KnowledgeCollectionFileMeta");

    public IMongoCollection<RoleDocument> Roles
        => GetCollectionOrCreate<RoleDocument>("Roles");

    public IMongoCollection<RoleAgentDocument> RoleAgents
        => GetCollectionOrCreate<RoleAgentDocument>("RoleAgents");

    public IMongoCollection<CrontabItemDocument> CrontabItems
        => GetCollectionOrCreate<CrontabItemDocument>("CronTabItems");

    public IMongoCollection<GlobalStatisticsDocument> GlobalStatistics
        => GetCollectionOrCreate<GlobalStatisticsDocument>("GlobalStatistics");

    public IMongoCollection<InstructionLogDocument> InstructionLogs
        => CreateInstructionLogIndex();
}