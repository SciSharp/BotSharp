using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Routing.Executor;
using BotSharp.Abstraction.Rules;
using BotSharp.Abstraction.Rules.Models;
using BotSharp.Core.Rules.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BotSharp.Core.UnitTests.Rules;

/// <summary>
/// 规则引擎曾是唯一不经过 FunctionExecutorFactory 的函数执行路径。测试集依赖"所有工具调用
/// 都能被接管"，漏掉这条路意味着规则触发的工具在测试期照样真实执行。
///
/// 第二个测试钉住的是这次改动最容易悄悄破坏的东西：原实现用 IsEqualTo 做大小写不敏感匹配，
/// 若换成工厂的大小写敏感匹配，配置里大小写不一致的规则会从"能跑"变成"找不到函数"。
/// </summary>
public class ToolCallActionTests
{
    private sealed class StubCallback(string name) : IFunctionCallback
    {
        public string Name => name;
        public bool Executed { get; private set; }
        public Task<bool> Execute(RoleDialogModel message)
        {
            Executed = true;
            message.Content = "real";
            return Task.FromResult(true);
        }
    }

    private sealed class ClaimingProvider : IFunctionExecutorProvider
    {
        public int Order => -1000;
        public bool Claimed { get; private set; }
        public IFunctionExecutor? TryResolve(string functionName, Agent agent)
        {
            Claimed = true;
            return new MockExecutor();
        }

        private sealed class MockExecutor : IFunctionExecutor
        {
            public Task<bool> ExecuteAsync(RoleDialogModel message)
            {
                message.Content = "mocked";
                return Task.FromResult(true);
            }
            public Task<string> GetIndicatorAsync(RoleDialogModel message) => Task.FromResult(string.Empty);
        }
    }

    /// <summary>
    /// Minimal <see cref="IRuleTrigger"/> implementation for these tests. EntityType/EntityId have
    /// no default implementation on the interface and must be supplied; Name is overridden because
    /// ToolCallAction's failure path formats an error message with trigger.Name, and the interface's
    /// default Name getter throws NotImplementedException.
    /// </summary>
    private sealed class StubTrigger : IRuleTrigger
    {
        public string EntityType { get; set; } = "test";
        public string EntityId { get; set; } = "test";
        public string Name => "stub_trigger";
    }

    private static (ToolCallAction action, IServiceProvider services) Build(
        IEnumerable<IFunctionCallback> callbacks,
        IEnumerable<IFunctionExecutorProvider> providers)
    {
        var services = new ServiceCollection();
        foreach (var cb in callbacks) services.AddSingleton(cb);
        foreach (var p in providers) services.AddSingleton(p);
        services.AddScoped<IFunctionExecutorFactory, BotSharp.Core.Routing.Executor.FunctionExecutorFactory>();
        var sp = services.BuildServiceProvider();
        return (new ToolCallAction(sp, NullLogger<ToolCallAction>.Instance), sp);
    }

    private static RuleFlowContext ContextFor(string functionName) => new()
    {
        Parameters = new() { ["function_name"] = functionName }
    };

    [Fact]
    public async Task A_provider_can_take_over_a_rule_triggered_tool_call()
    {
        var real = new StubCallback("create_work_order");
        var provider = new ClaimingProvider();
        var (action, _) = Build([real], [provider]);

        var result = await action.ExecuteAsync(new Agent { Name = "a" }, new StubTrigger(), ContextFor("create_work_order"));

        Assert.True(provider.Claimed);
        Assert.False(real.Executed);          // 真实实现一次都不能被调到
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Function_name_matching_stays_case_insensitive()
    {
        var real = new StubCallback("create_work_order");
        var (action, _) = Build([real], []);

        var result = await action.ExecuteAsync(new Agent { Name = "a" }, new StubTrigger(), ContextFor("Create_Work_Order"));

        Assert.True(real.Executed);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Reports_failure_when_the_function_cannot_be_resolved()
    {
        var (action, _) = Build([], []);

        var result = await action.ExecuteAsync(new Agent { Name = "a" }, new StubTrigger(), ContextFor("nope"));

        Assert.False(result.Success);
    }
}
