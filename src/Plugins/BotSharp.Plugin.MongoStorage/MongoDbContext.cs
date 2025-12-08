using BotSharp.Abstraction.Repositories.Settings;
using System.Threading;

namespace BotSharp.Plugin.MongoStorage;

public class MongoDbContext
{
    private readonly MongoClient _mongoClient;
    private readonly string _mongoDbDatabaseName;
    private readonly string _collectionPrefix;
    private static int _indexesInitialized = 0;

    private const string DB_NAME_INDEX = "authSource";

    public MongoDbContext(BotSharpDatabaseSettings dbSettings)
    {
        var mongoDbConnectionString = dbSettings.BotSharpMongoDb;
        _mongoClient = new MongoClient(mongoDbConnectionString);
        _mongoDbDatabaseName = GetDatabaseName(mongoDbConnectionString);
        _collectionPrefix = dbSettings.TablePrefix.IfNullOrEmptyAs("BotSharp")!;
        CreateIndexes();
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

    public void CreateIndexes()
    {
        // Use Interlocked.CompareExchange to ensure the index is initialized only once, ensuring thread safety
        // 0 indicates uninitialized, 1 indicates initialized
        if (Interlocked.CompareExchange(ref _indexesInitialized, 1, 0) != 0)
        {
            return;
        }

        // Perform index creation (only executed on the first call)
        CreateConversationIndex();
        CreateConversationStateIndex();
        CreateContentLogIndex();
        CreateStateLogIndex();
        CreateInstructionLogIndex();
        CreateAgentCodeScriptIndex();
        CreateAgentTaskIndex();
    }

    /// <summary>
    /// Gets all index names from a collection
    /// </summary>
    private HashSet<string> GetIndexNames<T>(IMongoCollection<T> collection)
    {
        var indexes = collection.Indexes.List().ToList();
        var indexNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var index in indexes)
        {
            if (index.Contains("name"))
            {
                var nameElement = index["name"];
                if (nameElement != BsonNull.Value && nameElement.IsString)
                {
                    indexNames.Add(nameElement.AsString);
                }
            }
        }
        
        return indexNames;
    }

    /// <summary>
    /// Checks if an index exists by field name pattern
    /// </summary>
    private bool IndexExistsByField<T>(IMongoCollection<T> collection, string fieldName)
    {
        var indexNames = GetIndexNames(collection);
        // MongoDB index names follow pattern: fieldName_1 or fieldName_-1 for ascending/descending
        return indexNames.Any(name => 
            name.Equals(fieldName + "_1", StringComparison.OrdinalIgnoreCase) ||
            name.Equals(fieldName + "_-1", StringComparison.OrdinalIgnoreCase) ||
            name.Equals(fieldName, StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith(fieldName + "_", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Creates an index if it doesn't exist, with error handling
    /// </summary>
    private void EnsureIndex<T>(IMongoCollection<T> collection, IndexKeysDefinition<T> indexKeyDef, string? indexName = null, CreateIndexOptions? options = null) where T : MongoBase
    {
        try
        {
            var indexOptions = options ?? new CreateIndexOptions();
            if (!string.IsNullOrWhiteSpace(indexName))
            {
                indexOptions.Name = indexName;
            }

            var indexModel = new CreateIndexModel<T>(indexKeyDef, indexOptions);
            collection.Indexes.CreateOne(indexModel);
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexOptionsConflict" || ex.CodeName == "IndexKeySpecsConflict" || ex.Code == 85)
        {
            // Index already exists with different options or duplicate key, which is acceptable
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Code == 85)
        {
            // Duplicate key error, index already exists
        }
    }

    private void CreateAgentCodeScriptIndex()
    {
        var collection = GetCollectionOrCreate<AgentCodeScriptDocument>("AgentCodeScripts");
        var indexNames = GetIndexNames(collection);

        bool IndexExists(string fieldName) => indexNames.Any(name => 
            name.Equals(fieldName + "_1", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith(fieldName + "_", StringComparison.OrdinalIgnoreCase));

        if (!IndexExists("AgentId"))
        {
            EnsureIndex(collection, Builders<AgentCodeScriptDocument>.IndexKeys.Ascending(x => x.AgentId), "AgentId_1");
        }

        if (!IndexExists("Name"))
        {
            EnsureIndex(collection, Builders<AgentCodeScriptDocument>.IndexKeys.Ascending(x => x.Name), "Name_1");
        }

        if (!IndexExists("ScriptType"))
        {
            EnsureIndex(collection, Builders<AgentCodeScriptDocument>.IndexKeys.Ascending(x => x.ScriptType), "ScriptType_1");
        }
    }

    private void CreateConversationIndex()
    {
        var collection = GetCollectionOrCreate<ConversationDocument>("Conversations");
        if (!IndexExistsByField(collection, "CreatedTime"))
        {
            EnsureIndex(collection, Builders<ConversationDocument>.IndexKeys.Descending(x => x.CreatedTime), "CreatedTime_-1");
        }
    }

    private void CreateConversationStateIndex()
    {
        var collection = GetCollectionOrCreate<ConversationStateDocument>("ConversationStates");
        if (!IndexExistsByField(collection, "States.Key"))
        {
            EnsureIndex(collection, Builders<ConversationStateDocument>.IndexKeys.Ascending("States.Key"), "States.Key_1");
        }
    }

    private void CreateAgentTaskIndex()
    {
        var collection = GetCollectionOrCreate<AgentTaskDocument>("AgentTasks");
        if (!IndexExistsByField(collection, "CreatedTime"))
        {
            EnsureIndex(collection, Builders<AgentTaskDocument>.IndexKeys.Descending(x => x.CreatedTime), "CreatedTime_-1");
        }
    }

    private void CreateContentLogIndex()
    {
        var collection = GetCollectionOrCreate<ConversationContentLogDocument>("ConversationContentLogs");
        if (!IndexExistsByField(collection, "CreatedTime"))
        {
            EnsureIndex(collection, Builders<ConversationContentLogDocument>.IndexKeys.Ascending(x => x.CreatedTime), "CreatedTime_1");
        }
    }

    private void CreateStateLogIndex()
    {
        var collection = GetCollectionOrCreate<ConversationStateLogDocument>("ConversationStateLogs");
        if (!IndexExistsByField(collection, "CreatedTime"))
        {
            EnsureIndex(collection, Builders<ConversationStateLogDocument>.IndexKeys.Ascending(x => x.CreatedTime), "CreatedTime_1");
        }
    }

    private void CreateInstructionLogIndex()
    {
        var collection = GetCollectionOrCreate<InstructionLogDocument>("InstructionLogs");
        if (!IndexExistsByField(collection, "CreatedTime"))
        {
            EnsureIndex(collection, Builders<InstructionLogDocument>.IndexKeys.Descending(x => x.CreatedTime), "CreatedTime_-1");
        }
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