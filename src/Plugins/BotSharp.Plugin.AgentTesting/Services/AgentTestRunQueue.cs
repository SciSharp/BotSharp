using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using BotSharp.Plugin.AgentTesting.Repositories;

namespace BotSharp.Plugin.AgentTesting.Services;

public interface IAgentTestRunQueue
{
    void Enqueue(string runId);
}

/// <summary>
/// An in-process, unbounded, single-consumer run queue: POST .../run only drops a Pending run's id
/// in here and returns immediately, and the real execution happens serially in this
/// BackgroundService's loop.
///
/// The DI shape follows WeChatBackgroundService elsewhere in this repo -- the one existing
/// precedent for "both a singleton and a BackgroundService": register the concrete type, have
/// AddHostedService forward to that same instance, then forward the interface type to it as well.
/// All three point at one object, so Enqueue and the background loop share a Channel.
///
/// A fresh DI scope per CASE, not per RUN -- see the comment on ScopedCaseRunner. Each dequeue here
/// opens only an outer scope that lives for the whole run, used solely to resolve
/// IAgentTestRepository and ILogger&lt;AgentTestRunExecutor&gt;, neither of which carries dangerous
/// cross-case state. The layer that would actually leak BotSharp scoped services between two cases
/// (IConversationService/IConversationStateService, and TestMockExecutorProvider's ambient
/// conversation id) is isolated in ScopedCaseRunner's own inner scope, reopened on every RunAsync.
/// </summary>
public class AgentTestRunQueue : BackgroundService, IAgentTestRunQueue
{
    private readonly Channel<string> _queue = Channel.CreateUnbounded<string>();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AgentTestRunQueue> _logger;

    public AgentTestRunQueue(IServiceProvider serviceProvider, ILogger<AgentTestRunQueue> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void Enqueue(string runId)
    {
        if (!_queue.Writer.TryWrite(runId))
        {
            _logger.LogError("Failed to enqueue agent test run {RunId}: the queue's writer rejected it.", runId);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The queue is in-process, so a restart wipes out every run still in flight. Without
        // sweeping them to Error they stay Running forever and the admin page keeps claiming they
        // are still going. Runs once per host start.
        await ReconcileStaleRunningRunsAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var runId = await _queue.Reader.ReadAsync(stoppingToken);
                await ProcessAsync(runId, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected failure in the agent test run queue loop.");
            }
        }
    }

    private async Task ReconcileStaleRunningRunsAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAgentTestRepository>();

            // Status-filtered at the query level, not ListRunsAsync(null) filtered in memory --
            // this collection only ever grows, and every host restart used to pull the entire
            // thing into memory just to find a handful of stale Running rows.
            var runs = await repo.ListRunsByStatusAsync(AgentTestStatus.Running);
            foreach (var run in runs)
            {
                run.Status = AgentTestStatus.Error;
                run.Error = "The host restarted while this run was still going, so it was abandoned. "
                    + "The queue is in-process and does not survive a restart -- trigger the run again.";
                run.CompletedAt = DateTime.UtcNow;
                await repo.UpdateRunAsync(run);
                _logger.LogWarning(
                    "Agent test run {RunId} was left Running by a previous process; marked Error on startup.",
                    run.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reconcile leftover Running agent test runs on startup.");
        }
    }

    private async Task ProcessAsync(string runId, CancellationToken ct)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAgentTestRepository>();
            var executorLogger = scope.ServiceProvider.GetRequiredService<ILogger<AgentTestRunExecutor>>();

            var executor = new AgentTestRunExecutor(repo, new ScopedCaseRunner(_serviceProvider), executorLogger);
            await executor.ExecuteAsync(runId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent test run {RunId} crashed outside of case-level handling.", runId);
            await TryMarkRunAsErrorAsync(runId, ex.Message);
        }
    }

    /// <param name="reason">
    /// Surfaced on the run itself. A crash outside case-level handling produces no case results at
    /// all, so without this the API reports Error with an empty result list and the reason exists
    /// only in this process's log.
    /// </param>
    private async Task TryMarkRunAsErrorAsync(string runId, string? reason = null)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAgentTestRepository>();

            var run = await repo.GetRunAsync(runId);
            if (run != null && run.Status != AgentTestStatus.Error)
            {
                run.Status = AgentTestStatus.Error;
                run.Error = string.IsNullOrWhiteSpace(reason)
                    ? "The run crashed before it could record a result."
                    : $"The run crashed before it could record a result: {reason}";
                run.CompletedAt = DateTime.UtcNow;
                await repo.UpdateRunAsync(run);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark agent test run {RunId} as Error after a queue-level crash.", runId);
        }
    }

    /// <summary>
    /// This is where "a fresh DI scope per case" actually happens.
    /// AgentTestRunExecutor.ExecuteAsync calls _caseRunner.RunAsync once per enabled case and
    /// neither knows nor cares whether the same ICaseRunner instance is behind it. This wrapper
    /// exploits that: it performs no orchestration of its own, and on every RunAsync call it opens
    /// a brand-new DI scope, resolves the real ICaseRunner from it (AgentTestCaseRunner, along with
    /// the whole scoped dependency chain it drags in -- IAgentConversationDriver,
    /// IConversationService, IConversationStateService, TestMockExecutorProvider), and disposes the
    /// scope once that one case is done.
    ///
    /// Why this is necessary rather than fastidious: TestMockExecutorProvider.TryResolve finds
    /// mocks by the ambient ConversationService._conversationId, not by an explicit argument, and
    /// ConversationStateService caches cross-turn state in memory. If several cases in one run
    /// shared a scope -- and therefore one IConversationService/IConversationStateService instance
    /// -- the next case's PrepareAsync would repoint the ambient conversation id at itself, and an
    /// orphaned call left over from a previous case's timeout could then unregister the entry the
    /// NEW case had just registered. The mock seam disappears and the orphaned call lands on the
    /// real tool implementation: a real phone call, a real email. A scope per case means those two
    /// BotSharp scoped services are never the same object across two cases, and the path does not
    /// exist.
    /// </summary>
    private sealed class ScopedCaseRunner : ICaseRunner
    {
        private readonly IServiceProvider _serviceProvider;

        public ScopedCaseRunner(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<AgentTestCaseResult> RunAsync(AgentTestSuite suite, AgentTestCase testCase, string runId, TestModel? model, CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<ICaseRunner>();
            return await runner.RunAsync(suite, testCase, runId, model, ct);
        }
    }
}
