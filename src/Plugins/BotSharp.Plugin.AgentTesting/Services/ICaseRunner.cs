namespace BotSharp.Plugin.AgentTesting.Services;

/// <summary>
/// 单用例运行器的接缝。抽出这一层是为了让 AgentTestRunExecutor 的编排逻辑
/// （串行、单条崩溃不终止、取消及时生效）可以脱离真 BotSharp 单元测试——
/// 也是为了让生产环境的实现可以按用例换成一个"每次调用都开新 DI scope"的包装，
/// 而不必改变 AgentTestRunExecutor 的构造签名。
/// </summary>
public interface ICaseRunner
{
    /// <param name="model">
    /// The model this execution is forced onto; null = use the agent's own LlmConfig (the existing
    /// behaviour when no models were requested).
    /// </param>
    Task<AgentTestCaseResult> RunAsync(AgentTestSuite suite, AgentTestCase testCase, string runId, TestModel? model, CancellationToken ct);
}
