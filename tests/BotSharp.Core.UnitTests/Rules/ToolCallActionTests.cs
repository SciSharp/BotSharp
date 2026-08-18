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
/// The rule engine used to be the only function-execution path that did not go through
/// FunctionExecutorFactory. The test set depends on every tool call being interceptable, and
/// missing this path means rule-triggered tools execute for real during a test.
///
/// The second test pins the thing this change could most easily have broken quietly: the original
/// matched names with IsEqualTo, which is case-insensitive. Switching to the factory's
/// case-sensitive comparison would turn a rule whose configured casing differs from "works" into
/// "no such function".
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

    /// <summary>
    /// Mirrors a plausible shape for a future mock/blocking provider keyed by function name (plan
    /// Task 5): a case-insensitive Dictionary lookup. Unlike HashSet&lt;T&gt;.Contains (which
    /// tolerates a null reference-type item via its own internal guard, verified separately -
    /// see the fix report), Dictionary&lt;TKey,TValue&gt; explicitly rejects a null key even for a
    /// read-only lookup: both TryGetValue and ContainsKey throw ArgumentNullException. So a null
    /// function_name reaching this provider crashes before it ever gets a chance to decide whether
    /// it claims the function - exactly the failure mode the null guard in ToolCallAction prevents.
    /// </summary>
    private sealed class NullIntolerantProvider : IFunctionExecutorProvider
    {
        private static readonly Dictionary<string, bool> _mockedNames =
            new(StringComparer.OrdinalIgnoreCase) { ["create_work_order"] = true };

        public int Order => 0;

        public IFunctionExecutor? TryResolve(string functionName, Agent agent)
            => _mockedNames.ContainsKey(functionName) ? throw new InvalidOperationException("unreachable") : null;
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
        Assert.False(real.Executed);          // the real implementation must never be reached
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

    /// <summary>
    /// A missing function_name must fail gracefully instead of reaching the factory seam with a
    /// null - the old IsEqualTo-based lookup was null-safe and simply matched nothing, and that
    /// graceful-failure contract must survive routing execution through the factory. The
    /// NullIntolerantProvider proves this isn't just an assertion on Success: without the guard,
    /// this test throws ArgumentNullException instead of returning a result at all.
    /// </summary>
    [Fact]
    public async Task Reports_failure_without_throwing_when_function_name_is_missing()
    {
        var (action, _) = Build([], [new NullIntolerantProvider()]);

        var result = await action.ExecuteAsync(new Agent { Name = "a" }, new StubTrigger(), new RuleFlowContext());

        Assert.False(result.Success);
    }
}
