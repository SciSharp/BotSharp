using BotSharp.Abstraction.Repositories.Settings;
using MongoDB.Driver;

namespace BotSharp.Plugin.AgentTesting.Models;

/// <summary>
/// The four collections this harness owns. Mirrors BotSharp.Plugin.MongoStorage.MongoDbContext:
/// same connection-string setting (<c>Database:BotSharpMongoDb</c>), same "derive the database name
/// from the connection string" rule, same TablePrefix convention -- so a host that already has Mongo
/// storage configured needs no additional configuration for the test set.
///
/// Kept separate from BotSharp's own storage abstraction on purpose. Adding these collections to
/// IBotSharpRepository would mean ~20 new members that FileRepository and BotSharpDbContext would
/// each have to implement, for data no other feature reads.
/// </summary>
public class AgentTestMongoDbContext
{
    private const string DefaultTablePrefix = "BotSharp";

    private readonly IMongoDatabase _database;
    private readonly string _collectionPrefix;

    public AgentTestMongoDbContext(BotSharpDatabaseSettings dbSettings)
    {
        var connectionString = dbSettings?.BotSharpMongoDb;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "The agent testing plugin needs Database:BotSharpMongoDb to be configured.");
        }

        var url = new MongoUrl(connectionString);
        var databaseName = string.IsNullOrEmpty(url.DatabaseName) ? url.AuthenticationSource : url.DatabaseName;
        _database = new MongoClient(connectionString).GetDatabase(databaseName);
        _collectionPrefix = string.IsNullOrEmpty(dbSettings?.TablePrefix) ? DefaultTablePrefix : dbSettings.TablePrefix;
    }

    private IMongoCollection<T> Collection<T>(string name) => _database.GetCollection<T>($"{_collectionPrefix}_{name}");

    public IMongoCollection<AgentTestSuite> AgentTestSuites => Collection<AgentTestSuite>("AgentTestSuites");
    public IMongoCollection<AgentTestCase> AgentTestCases => Collection<AgentTestCase>("AgentTestCases");
    public IMongoCollection<AgentTestRun> AgentTestRuns => Collection<AgentTestRun>("AgentTestRuns");
    public IMongoCollection<AgentTestCaseResult> AgentTestCaseResults => Collection<AgentTestCaseResult>("AgentTestCaseResults");
}
