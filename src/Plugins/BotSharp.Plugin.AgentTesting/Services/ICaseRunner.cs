namespace BotSharp.Plugin.AgentTesting.Services;

/// <summary>
/// The seam for running one case. Extracted so AgentTestRunExecutor's orchestration -- serial
/// execution, one crashing case not aborting the run, cancellation taking effect promptly -- can be
/// unit-tested without a real BotSharp, and so the production implementation can be swapped for a
/// wrapper that opens a fresh DI scope per call without changing AgentTestRunExecutor's constructor
/// signature.
/// </summary>
public interface ICaseRunner
{
    /// <param name="model">
    /// The model this execution is forced onto; null = use the agent's own LlmConfig (the existing
    /// behaviour when no models were requested).
    /// </param>
    Task<AgentTestCaseResult> RunAsync(AgentTestSuite suite, AgentTestCase testCase, string runId, TestModel? model, CancellationToken ct);
}
