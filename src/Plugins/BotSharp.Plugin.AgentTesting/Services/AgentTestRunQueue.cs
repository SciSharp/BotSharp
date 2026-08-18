using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using BotSharp.Plugin.AgentTesting.Repositories;

namespace BotSharp.Plugin.AgentTesting.Services;

public interface IAgentTestRunQueue
{
    void Enqueue(string runId);
}

/// <summary>
/// 进程内、无界、单消费者的 Run 队列：POST .../run 只管把一个 Pending Run 的 id 丢进来就
/// 立刻返回，真正执行放到这个 BackgroundService 的后台循环里串行处理。
///
/// DI 形状照 BotSharp.Plugin.WeChat 的 WeChatBackgroundService 那一套（同一份仓库里唯一一个
/// "既是单例又是 BackgroundService" 的先例）：注册一次具体类型 + AddHostedService 转发同一个
/// 实例 + 用接口类型再转发一次，三行都指向同一个对象，Enqueue 和后台循环用的是同一份 Channel。
///
/// 每个 CASE 一个新 DI scope，而不是每个 RUN 一个：见 ScopedCaseRunner 上的注释。这个类自己
/// 每次 dequeue 只开一个"跑这一整个 Run 期间"的外层 scope,只用来解析 IAgentTestRepository/
/// ILogger&lt;AgentTestRunExecutor&gt; 这两个没有跨 case 危险状态的东西；真正会在两个 case 之间
/// 泄漏 BotSharp scoped 服务（IConversationService/IConversationStateService/
/// TestMockExecutorProvider 的 ambient conversation id）的那一层，被隔到 ScopedCaseRunner
/// 自己的、每次 RunAsync 调用都重开一次的内层 scope 里。
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
        // 进程内队列，一次重启就会把所有还在跑的 Run 冲没——不把它们清成 Error，它们会永远停在
        // Running，管理页会一直显示"还在跑"。这一步只在 host 每次启动时跑一次。
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
            await TryMarkRunAsErrorAsync(runId);
        }
    }

    private async Task TryMarkRunAsErrorAsync(string runId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAgentTestRepository>();

            var run = await repo.GetRunAsync(runId);
            if (run != null && run.Status != AgentTestStatus.Error)
            {
                run.Status = AgentTestStatus.Error;
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
    /// 这就是"每个 case 一个新 DI scope"真正落地的地方。AgentTestRunExecutor.ExecuteAsync 对
    /// 启用的每一条用例都会调一次 _caseRunner.RunAsync——它自己完全不知道、也不关心这背后是不
    /// 是同一个 ICaseRunner 实例。这个包装类利用了这一点：它自己不做任何编排逻辑，只在每次
    /// RunAsync 被调用时开一个全新的 DI scope、从这个新 scope 里解析真正的 ICaseRunner
    /// （AgentTestCaseRunner，连同它带出来的 IAgentConversationDriver/IConversationService/
    /// IConversationStateService/TestMockExecutorProvider 一整条 scoped 依赖链），跑完这一个
    /// case 就释放这个 scope。
    ///
    /// 为什么这么做是必须的、不是洁癖：TestMockExecutorProvider.TryResolve 是按"当前
    /// ConversationService._conversationId 这个 ambient 值"找 mock，不是按显式传参；
    /// ConversationStateService 把跨轮的 state 缓存在内存里。如果一个 Run 里的多个 case
    /// 共用同一个 scope（也就是共用同一个 IConversationService/IConversationStateService
    /// 实例），后一个 case 的 PrepareAsync 会把 ambient conversation id 重新指到自己头上，
    /// 这时如果前一个 case 有过一次超时孤儿调用还没跑完，它 unregister 的时候可能摘掉的是
    /// 后一个 case 刚注册的条目——mock 接缝消失，孤儿调用落到真实工具实现上（真拨电话、真发
    /// 邮件）。给每个 case 单开一个 scope，这两个 BotSharp scoped 服务在任何两个 case 之间
    /// 永远不是同一个对象，这条路径就不存在了。
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
