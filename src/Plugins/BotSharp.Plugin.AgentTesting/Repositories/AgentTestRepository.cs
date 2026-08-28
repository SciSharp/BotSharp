using MongoDB.Driver;

namespace BotSharp.Plugin.AgentTesting.Repositories;

/// <summary>
/// The Mongo repository contract for this plugin's four document types (Suite/Case/Run/CaseResult).
/// The signatures -- in particular which parameters are string? rather than string -- match the
/// InMemoryRepo fake used by the tests, which is the specification this interface follows.
/// </summary>
public interface IAgentTestRepository
{
    Task<AgentTestSuite?> GetSuiteAsync(string id);
    Task<List<AgentTestSuite>> ListSuitesAsync(string? agentId);
    Task UpsertSuiteAsync(AgentTestSuite suite);
    Task DeleteSuiteAsync(string id);

    Task<AgentTestCase?> GetCaseAsync(string id);
    Task<List<AgentTestCase>> ListCasesAsync(string suiteId);
    Task UpsertCaseAsync(AgentTestCase testCase);
    Task DeleteCaseAsync(string id);

    Task<AgentTestRun> CreateRunAsync(AgentTestRun run);
    Task<AgentTestRun?> GetRunAsync(string id);
    Task<List<AgentTestRun>> ListRunsAsync(string? suiteId);

    /// <summary>
    /// Bounded counterpart to <see cref="ListRunsAsync"/> for the startup reconciliation sweep
    /// (AgentTestRunQueue.ReconcileStaleRunningRunsAsync), which only ever cares about one status
    /// (AgentTestStatus.Running) across every suite -- ListRunsAsync(null) would otherwise pull
    /// the entire, ever-growing AgentTestRuns collection into memory on every host startup just to
    /// filter it down to a handful of rows client-side.
    /// </summary>
    Task<List<AgentTestRun>> ListRunsByStatusAsync(string status);

    Task UpdateRunAsync(AgentTestRun run);

    /// <summary>
    /// Removes a run and every case result belonging to it, and reports how many results went with
    /// it.
    ///
    /// The cascade is the point. Case results are keyed by run id and are reachable no other way, so
    /// deleting the run alone would leave rows nothing can ever list, read or clean up again -- and
    /// every later aggregate that scans results would keep counting them.
    /// </summary>
    Task<long> DeleteRunAsync(string id);

    Task AddCaseResultAsync(AgentTestCaseResult result);
    Task<List<AgentTestCaseResult>> ListCaseResultsAsync(string runId);
}

public class AgentTestRepository : IAgentTestRepository
{
    private readonly AgentTestMongoDbContext _mongoDbContext;

    public AgentTestRepository(AgentTestMongoDbContext mongoDbContext)
    {
        _mongoDbContext = mongoDbContext;
    }

    public async Task<AgentTestSuite?> GetSuiteAsync(string id)
        => await _mongoDbContext.AgentTestSuites.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task<List<AgentTestSuite>> ListSuitesAsync(string? agentId)
    {
        var filter = string.IsNullOrWhiteSpace(agentId)
            ? Builders<AgentTestSuite>.Filter.Empty
            : Builders<AgentTestSuite>.Filter.Eq(x => x.AgentId, agentId);

        return await _mongoDbContext.AgentTestSuites
            .Find(filter)
            .SortByDescending(x => x.CreateDate)
            .ToListAsync();
    }

    public async Task UpsertSuiteAsync(AgentTestSuite suite)
    {
        // ReplaceOneAsync(upsert:true) does not run the [BsonId(IdGenerator=...)] hook the way
        // InsertOneAsync does -- a brand-new document with a null/empty Id must get a real one
        // here, or the driver would send an upsert whose replacement document has no _id at all
        // (see the same fix already applied in UnableToValidateJobMongoRepository.UpsertAsync).
        if (string.IsNullOrEmpty(suite.Id))
        {
            suite.Id = Guid.NewGuid().ToString();
        }

        suite.UpdateDate = DateTime.UtcNow;

        await _mongoDbContext.AgentTestSuites.ReplaceOneAsync(
            x => x.Id == suite.Id,
            suite,
            new ReplaceOptions { IsUpsert = true });
    }

    public async Task DeleteSuiteAsync(string id)
        => await _mongoDbContext.AgentTestSuites.DeleteOneAsync(x => x.Id == id);

    public async Task<AgentTestCase?> GetCaseAsync(string id)
        => await _mongoDbContext.AgentTestCases.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task<List<AgentTestCase>> ListCasesAsync(string suiteId)
        => await _mongoDbContext.AgentTestCases
            .Find(x => x.SuiteId == suiteId)
            .SortByDescending(x => x.CreateDate)
            .ToListAsync();

    public async Task UpsertCaseAsync(AgentTestCase testCase)
    {
        if (string.IsNullOrEmpty(testCase.Id))
        {
            testCase.Id = Guid.NewGuid().ToString();
        }

        testCase.UpdateDate = DateTime.UtcNow;

        await _mongoDbContext.AgentTestCases.ReplaceOneAsync(
            x => x.Id == testCase.Id,
            testCase,
            new ReplaceOptions { IsUpsert = true });
    }

    public async Task DeleteCaseAsync(string id)
        => await _mongoDbContext.AgentTestCases.DeleteOneAsync(x => x.Id == id);

    public async Task<AgentTestRun> CreateRunAsync(AgentTestRun run)
    {
        // InsertOneAsync DOES run the StringGuidIdGenerator hook for a null/empty Id, unlike the
        // upsert path above -- but setting it explicitly here as well costs nothing and means the
        // caller (the controller) can read back run.Id immediately after this call returns, before
        // the insert has even happened, e.g. to log it.
        if (string.IsNullOrEmpty(run.Id))
        {
            run.Id = Guid.NewGuid().ToString();
        }

        await _mongoDbContext.AgentTestRuns.InsertOneAsync(run);
        return run;
    }

    public async Task<AgentTestRun?> GetRunAsync(string id)
        => await _mongoDbContext.AgentTestRuns.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task<List<AgentTestRun>> ListRunsAsync(string? suiteId)
    {
        var filter = string.IsNullOrWhiteSpace(suiteId)
            ? Builders<AgentTestRun>.Filter.Empty
            : Builders<AgentTestRun>.Filter.Eq(x => x.SuiteId, suiteId);

        return await _mongoDbContext.AgentTestRuns
            .Find(filter)
            .SortByDescending(x => x.CreateDate)
            .ToListAsync();
    }

    public async Task<List<AgentTestRun>> ListRunsByStatusAsync(string status)
        => await _mongoDbContext.AgentTestRuns
            .Find(x => x.Status == status)
            .ToListAsync();

    public async Task UpdateRunAsync(AgentTestRun run)
        => await _mongoDbContext.AgentTestRuns.ReplaceOneAsync(x => x.Id == run.Id, run);

    public async Task<long> DeleteRunAsync(string id)
    {
        // Results first. If the process dies between the two deletes, an orphaned RUN is visible and
        // deletable again; orphaned RESULTS are not reachable at all once their run is gone. Losing
        // the recoverable half is the better failure.
        var results = await _mongoDbContext.AgentTestCaseResults.DeleteManyAsync(x => x.RunId == id);
        await _mongoDbContext.AgentTestRuns.DeleteOneAsync(x => x.Id == id);
        return results.DeletedCount;
    }

    public async Task AddCaseResultAsync(AgentTestCaseResult result)
    {
        if (string.IsNullOrEmpty(result.Id))
        {
            result.Id = Guid.NewGuid().ToString();
        }

        await _mongoDbContext.AgentTestCaseResults.InsertOneAsync(result);
    }

    public async Task<List<AgentTestCaseResult>> ListCaseResultsAsync(string runId)
        // Ascending, deliberately unlike every other List*Async method in this file: those are
        // "list of independent rows for an admin screen," newest-first. This one is "the case
        // results FOR ONE RUN," where the natural read order is execution order -- newest-first
        // would show a run's cases back-to-front, and ties within the same millisecond would sort
        // nondeterministically either way.
        => await _mongoDbContext.AgentTestCaseResults
            .Find(x => x.RunId == runId)
            .SortBy(x => x.CreateDate)
            .ToListAsync();
}
