using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Routing.Executor;
using BotSharp.Core.Routing.Executor;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BotSharp.Core.UnitTests.Routing;

/// <summary>
/// This factory is the one place in the repo that decides who executes a given function name, and
/// therefore the only place a tool can be swapped for a fake. It used to be internal static, so
/// nothing outside could take part in resolution -- and the test-set feature has to intercept real
/// tools inside a test conversation, or running the regression suite once really does send emails
/// and really does create work orders.
///
/// These tests also pin compatibility: with no provider registered, resolution must match the
/// pre-change behaviour exactly. That is the premise on which touching this core path was
/// acceptable at all, and the one way it could introduce a silent regression.
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

    /// <summary>
    /// Mirrors BotSharp.Core.Rules.ToolCallActionTests.NullIntolerantProvider: a case-insensitive
    /// Dictionary&lt;string,...&gt; lookup, the shape a real mock/blocking provider plausibly takes.
    /// Dictionary.ContainsKey/TryGetValue throw ArgumentNullException on a null key even for a
    /// read-only lookup, so this proves the factory's own guard -- not just a lucky provider
    /// implementation -- is what keeps a null/blank functionName from reaching TryResolve at all.
    /// </summary>
    private sealed class NullIntolerantProvider : IFunctionExecutorProvider
    {
        private static readonly Dictionary<string, bool> _mockedNames =
            new(StringComparer.OrdinalIgnoreCase) { ["create_work_order"] = true };

        public int Order => 0;
        public bool WasAsked { get; private set; }

        public IFunctionExecutor? TryResolve(string functionName, Agent agent)
        {
            WasAsked = true;
            return _mockedNames.ContainsKey(functionName) ? throw new InvalidOperationException("unreachable") : null;
        }
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
        // Compatibility guard: this was the only behaviour before the change, and must survive it.
        var factory = BuildFactory([new StubCallback("create_work_order")], []);

        var executor = factory.Create("create_work_order", new Agent());

        Assert.IsType<FunctionCallbackExecutor>(executor);
    }

    [Fact]
    public void Providers_are_asked_in_ascending_order_and_the_first_claim_wins()
    {
        var early = new StubProvider(-100, "create_work_order");
        var late = new StubProvider(100, "create_work_order");
        // Registered late-first on purpose, so a passing test proves the ordering is real rather
        // than an accident of registration order.
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

    /// <summary>
    /// The factory is documented as the single trusted seam every function-call path must go
    /// through -- it cannot depend on every current and future caller pre-validating for it.
    /// RoutingService.InvokeFunction has no guard of its own before calling Create(name, ...), so
    /// a null/blank name is a real, reachable input here, not a hypothetical.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Returns_null_for_a_null_or_blank_function_name_without_asking_any_provider(string? functionName)
    {
        var provider = new NullIntolerantProvider();
        var factory = BuildFactory([], [provider]);

        var executor = factory.Create(functionName!, new Agent());

        Assert.Null(executor);
        Assert.False(provider.WasAsked);
    }
}
