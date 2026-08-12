using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Routing.Executor;
using BotSharp.Core.Routing.Executor;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BotSharp.Core.UnitTests.Routing;

/// <summary>
/// 这个工厂是全仓唯一决定"某个函数名由谁执行"的地方，因此也是唯一能把工具替换成假实现的地方。
/// 它原本是 internal static，外部无法参与解析；测试集功能需要在测试会话里拦下真实工具，
/// 否则跑一遍回归就会真发邮件、真建工单。
///
/// 这里同时钉住兼容性：没有 provider 注册时，解析结果必须与改动前一致——这是这次改动
/// 敢动核心路径的前提，也是它唯一可能引入静默回归的地方。
/// </summary>
public class FunctionExecutorFactoryTests
{
    private sealed class StubCallback(string name) : IFunctionCallback
    {
        public string Name => name;
        public Task<bool> Execute(RoleDialogModel message) => Task.FromResult(true);
    }

    private sealed class StubExecutor : IFunctionExecutor
    {
        public Task<bool> ExecuteAsync(RoleDialogModel message) => Task.FromResult(true);
        public Task<string> GetIndicatorAsync(RoleDialogModel message) => Task.FromResult(string.Empty);
    }

    private sealed class StubProvider(int order, string? claims) : IFunctionExecutorProvider
    {
        public int Order => order;
        public IFunctionExecutor? TryResolve(string functionName, Agent agent)
            => functionName == claims ? Executor : null;
        public StubExecutor Executor { get; } = new();
    }

    private static IFunctionExecutorFactory BuildFactory(
        IEnumerable<IFunctionCallback> callbacks,
        IEnumerable<IFunctionExecutorProvider> providers)
    {
        var services = new ServiceCollection();
        foreach (var cb in callbacks) services.AddSingleton(cb);
        foreach (var p in providers) services.AddSingleton(p);
        var sp = services.BuildServiceProvider();
        return new FunctionExecutorFactory(sp);
    }

    [Fact]
    public void Provider_takes_precedence_over_a_registered_callback()
    {
        var provider = new StubProvider(0, "create_work_order");
        var factory = BuildFactory([new StubCallback("create_work_order")], [provider]);

        var executor = factory.Create("create_work_order", new Agent());

        Assert.Same(provider.Executor, executor);
    }

    [Fact]
    public void Falls_back_to_the_registered_callback_when_no_provider_claims_it()
    {
        // 兼容性保护：这是改动前唯一的行为，必须原样保留。
        var factory = BuildFactory([new StubCallback("create_work_order")], []);

        var executor = factory.Create("create_work_order", new Agent());

        Assert.IsType<FunctionCallbackExecutor>(executor);
    }

    [Fact]
    public void Providers_are_asked_in_ascending_order_and_the_first_claim_wins()
    {
        var early = new StubProvider(-100, "create_work_order");
        var late = new StubProvider(100, "create_work_order");
        // 故意按"晚的先注册"的顺序放，确保排序不是靠注册顺序碰巧对的。
        var factory = BuildFactory([], [late, early]);

        var executor = factory.Create("create_work_order", new Agent());

        Assert.Same(early.Executor, executor);
    }

    [Fact]
    public void Returns_null_when_nobody_can_execute_the_function()
    {
        var factory = BuildFactory([], []);

        Assert.Null(factory.Create("no_such_function", new Agent()));
    }
}
