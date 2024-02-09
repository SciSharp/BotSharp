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

    public IMongoCollection<AgentDocument> Agents
        => Database.GetCollection<AgentDocument>($"{_collectionPrefix}_Agents");

    public IMongoCollection<AgentTaskDocument> AgentTasks
        => Database.GetCollection<AgentTaskDocument>($"{_collectionPrefix}_AgentTasks");

    public IMongoCollection<ConversationDocument> Conversations
        => Database.GetCollection<ConversationDocument>($"{_collectionPrefix}_Conversations");

    public IMongoCollection<ConversationDialogDocument> ConversationDialogs
        => Database.GetCollection<ConversationDialogDocument>($"{_collectionPrefix}_ConversationDialogs");

    public IMongoCollection<ConversationStateDocument> ConversationStates
        => Database.GetCollection<ConversationStateDocument>($"{_collectionPrefix}_ConversationStates");

    public IMongoCollection<ExecutionLogDocument> ExectionLogs
        => Database.GetCollection<ExecutionLogDocument>($"{_collectionPrefix}_ExecutionLogs");

    public IMongoCollection<UserDocument> Users
        => Database.GetCollection<UserDocument>($"{_collectionPrefix}_Users");

    public IMongoCollection<UserAgentDocument> UserAgents
        => Database.GetCollection<UserAgentDocument>($"{_collectionPrefix}_UserAgents");

    public IMongoCollection<LlmCompletionLogDocument> LlmCompletionLogs
        => Database.GetCollection<LlmCompletionLogDocument>($"{_collectionPrefix}_Llm_Completion_Logs");

    public IMongoCollection<PluginDocument> Plugins
        => Database.GetCollection<PluginDocument>($"{_collectionPrefix}_Plugins");
}
