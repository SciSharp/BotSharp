using BotSharp.Abstraction.Repositories.Settings;
using BotSharp.Abstraction.Utilities;
using System.Threading;

namespace BotSharp.Plugin.MongoStorage;

public class MongoDbContext
{
    private const string DefaultTablePrefix = "BotSharp";
    private readonly MongoClient _mongoClient;
    private readonly string _mongoDbDatabaseName;
    private readonly string _collectionPrefix;
    private static int _indexesInitialized = 0;

    public MongoDbContext(BotSharpDatabaseSettings dbSettings)
    {
        var mongoDbConnectionString = dbSettings?.BotSharpMongoDb;
        if (string.IsNullOrWhiteSpace(mongoDbConnectionString))
            throw new InvalidOperationException("MongoDB is enabled but BotSharpMongoDb is not configured.");

        _mongoClient = new MongoClient(mongoDbConnectionString);
        _mongoDbDatabaseName = GetDatabaseName(mongoDbConnectionString);
        _collectionPrefix = dbSettings?.TablePrefix.IfNullOrEmptyAs(DefaultTablePrefix)!;
        CreateIndexes();
    }

    private string GetDatabaseName(string mongoDbConnectionString)
    {
        var mongoUrl = new MongoUrl(mongoDbConnectionString);
        var databaseName = mongoUrl.DatabaseName.IfNullOrEmptyAs(mongoUrl.AuthenticationSource);
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException(
                "MongoDB connection string must specify a database: set the database in the path (e.g. mongodb://host/dbname) or set authSource in the query string (e.g. ?authSource=dbname). ");
        }
        return databaseName!;
    }

    private IMongoDatabase Database => _mongoClient.GetDatabase(_mongoDbDatabaseName);

    #region Private methods
    private bool CollectionExists(IMongoDatabase database, string collectionName)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("name", collectionName);
        var collections = database.ListCollectionNames(new ListCollectionNamesOptions { Filter = filter }).ToList();
        return collections.Any();
    }

    private IMongoCollection<TDocument> GetCollection<TDocument>(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"The collection {name} cannot be empty.");
        }

        var collectionName = $"{_collectionPrefix}_{name}";

        var collection = Database.GetCollection<TDocument>(collectionName);
        return collection;
    }

    private IMongoCollection<TDocument> GetCollectionOrCreate<TDocument>(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"The collection {name} cannot be empty.");
        }

        var collectionName = $"{_collectionPrefix}_{name}";
        if (!CollectionExists(Database, collectionName))
        {
            Database.CreateCollection(collectionName);
        }

        var collection = Database.GetCollection<TDocument>(collectionName);
        return collection;
    }

    #region Indexes
    private void CreateIndexes()
    {
        // Use Interlocked.CompareExchange to ensure the index is initialized only once, ensuring thread safety
        // 0 indicates uninitialized, 1 indicates initialized
        if (Interlocked.CompareExchange(ref _indexesInitialized, 1, 0) != 0)
        {
            return;
        }

        // Perform index creation (only executed on the first call).
        CreateConversationIndex();
        CreateConversationStateIndex();
        CreateContentLogIndex();
        CreateStateLogIndex();
        CreateInstructionLogIndex();
        CreateAgentCodeScriptIndex();
        CreateAgentTaskIndex();
    }

    private IMongoCollection<AgentCodeScriptDocument> CreateAgentCodeScriptIndex()
    {
        var collection = GetCollectionOrCreate<AgentCodeScriptDocument>("AgentCodeScripts");
        var curIndexes = collection.Indexes.List().ToList().Where(x => x.Contains("name")).Select(x => x["name"].AsString);

        if (!curIndexes.Any(x => x.StartsWith("AgentId")))
        {
            CreateIndex(collection, Builders<AgentCodeScriptDocument>.IndexKeys.Ascending(x => x.AgentId));
        }

        if (!curIndexes.Any(x => x.StartsWith("Name")))
        {
            CreateIndex(collection, Builders<AgentCodeScriptDocument>.IndexKeys.Ascending(x => x.Name));
        }

        if (!curIndexes.Any(x => x.StartsWith("ScriptType")))
        {
            CreateIndex(collection, Builders<AgentCodeScriptDocument>.IndexKeys.Ascending(x => x.ScriptType));
        }

        return collection;
    }

    private IMongoCollection<ConversationDocument> CreateConversationIndex()
    {
        var collection = GetCollectionOrCreate<ConversationDocument>("Conversations");
        var indexes = collection.Indexes.List().ToList();
        var createTimeIndex = indexes.FirstOrDefault(x => x.GetElement("name").ToString().StartsWith("CreatedTime"));
        if (createTimeIndex == null)
        {
            CreateIndex(collection, Builders<ConversationDocument>.IndexKeys.Descending(x => x.CreatedTime));
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
            CreateIndex(collection, Builders<ConversationStateDocument>.IndexKeys.Ascending("States.Key"));
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
            CreateIndex(collection, Builders<AgentTaskDocument>.IndexKeys.Descending(x => x.CreatedTime));
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
            CreateIndex(collection, Builders<ConversationContentLogDocument>.IndexKeys.Ascending(x => x.CreatedTime));
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
            CreateIndex(collection, Builders<ConversationStateLogDocument>.IndexKeys.Ascending(x => x.CreatedTime));
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
            CreateIndex(collection, Builders<InstructionLogDocument>.IndexKeys.Descending(x => x.CreatedTime));
        }
        return collection;
    }

    private void CreateIndex<T>(IMongoCollection<T> collection, IndexKeysDefinition<T> indexKeyDef, CreateIndexOptions? options = null) where T : MongoBase
    {
        collection.Indexes.CreateOne(new CreateIndexModel<T>(indexKeyDef, options));
    }
    #endregion
    #endregion

    public IMongoCollection<AgentDocument> Agents
        => GetCollection<AgentDocument>("Agents");

    public IMongoCollection<AgentTaskDocument> AgentTasks
        => GetCollection<AgentTaskDocument>("AgentTasks");

    public IMongoCollection<AgentCodeScriptDocument> AgentCodeScripts
        => GetCollection<AgentCodeScriptDocument>("AgentCodeScripts");

    public IMongoCollection<ConversationDocument> Conversations
        => GetCollection<ConversationDocument>("Conversations");

    public IMongoCollection<ConversationDialogDocument> ConversationDialogs
        => GetCollection<ConversationDialogDocument>("ConversationDialogs");

    public IMongoCollection<ConversationStateDocument> ConversationStates
        => GetCollection<ConversationStateDocument>("ConversationStates");

    public IMongoCollection<ConversationFileDocument> ConversationFiles
        => GetCollection<ConversationFileDocument>("ConversationFiles");

    public IMongoCollection<LlmCompletionLogDocument> LlmCompletionLogs
        => GetCollection<LlmCompletionLogDocument>("LlmCompletionLogs");

    public IMongoCollection<ConversationContentLogDocument> ContentLogs
        => GetCollection<ConversationContentLogDocument>("ConversationContentLogs");

    public IMongoCollection<ConversationStateLogDocument> StateLogs
        => GetCollection<ConversationStateLogDocument>("ConversationStateLogs");

    public IMongoCollection<UserDocument> Users
        => GetCollection<UserDocument>("Users");

    public IMongoCollection<UserAgentDocument> UserAgents
        => GetCollection<UserAgentDocument>("UserAgents");

    public IMongoCollection<PluginDocument> Plugins
        => GetCollection<PluginDocument>("Plugins");

    public IMongoCollection<TranslationMemoryDocument> TranslationMemories
        => GetCollection<TranslationMemoryDocument>("TranslationMemories");

    public IMongoCollection<KnowledgeCollectionConfigDocument> KnowledgeCollectionConfigs
        => GetCollection<KnowledgeCollectionConfigDocument>("KnowledgeCollectionConfigs");

    public IMongoCollection<KnowledgeCollectionFileMetaDocument> KnowledgeCollectionFileMeta
        => GetCollection<KnowledgeCollectionFileMetaDocument>("KnowledgeCollectionFileMeta");

    public IMongoCollection<RoleDocument> Roles
        => GetCollection<RoleDocument>("Roles");

    public IMongoCollection<RoleAgentDocument> RoleAgents
        => GetCollection<RoleAgentDocument>("RoleAgents");

    public IMongoCollection<CrontabItemDocument> CrontabItems
        => GetCollection<CrontabItemDocument>("CronTabItems");

    public IMongoCollection<GlobalStatisticsDocument> GlobalStats
        => GetCollection<GlobalStatisticsDocument>("GlobalStats");

    public IMongoCollection<InstructionLogDocument> InstructionLogs
        => GetCollection<InstructionLogDocument>("InstructionLogs");
}