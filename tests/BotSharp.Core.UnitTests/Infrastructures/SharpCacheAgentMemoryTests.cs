using System.Reflection;
using System.Runtime.CompilerServices;
using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Abstraction.Infrastructures;
using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Routing.Settings;
using BotSharp.Abstraction.Rules;
using BotSharp.Abstraction.Utilities;
using BotSharp.Core.Agents.Services;
using BotSharp.Core.Infrastructures;
using BotSharp.Core.Plugins;
using BotSharp.Core.Rules.Engines;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace BotSharp.Core.UnitTests.Infrastructures;

/// <summary>
/// Guards against the OneBrain memory saw-tooth (1.8-2.8 GB against a 3 GiB limit, GTR-13556).
/// <para>
/// <see cref="SharpCacheAttribute"/> used to key reference-type arguments by <c>GetHashCode()</c>, i.e. object
/// identity. <see cref="RuleEngine.Triggered"/> builds a new <see cref="AgentFilter"/> on every call, so each
/// trigger missed and parked another full copy of every agent in the static MemoryCache for 10 minutes.
/// <c>perInstanceCache</c> entries were also parked there, one dead copy per DI scope.
/// </para>
/// Uses the real, Rougamo-woven <see cref="AgentService"/>; only the repository is mocked, returning fresh
/// objects on every call the way the Mongo driver deserializes them.
/// </summary>
public class SharpCacheAgentMemoryTests : IDisposable
{
    // Roughly the shape of OneBrain's agent collection: ~60 agents, ~2 MB of JSON on disk.
    private const int AgentCount = 60;
    private const int InstructionChars = 8_000;
    private const int TemplateChars = 4_000;
    private const int Calls = 50;

    private readonly ITestOutputHelper _output;

    public SharpCacheAgentMemoryTests(ITestOutputHelper output)
    {
        _output = output;
        ClearSharpCache();
    }

    public void Dispose() => ClearSharpCache();

    [Fact]
    public async Task GetAgents_with_equal_new_filters_reads_once_and_caches_one_copy()
    {
        var (provider, db) = Build();
        var agentService = provider.GetRequiredService<IAgentService>();

        var entriesBefore = SharpCacheEntryCount();
        var bytesBefore = RetainedBytes();

        for (var i = 0; i < Calls; i++)
        {
            // Exactly what RuleEngine.Triggered does on every call.
            await agentService.GetAgents(new AgentFilter { Pager = new Pagination { Size = 1000 } });
        }

        var retained = RetainedBytes() - bytesBefore;
        _output.WriteLine($"[new filter per call] calls={Calls}, repo reads={ReadCount(db)}, cache entries added={SharpCacheEntryCount() - entriesBefore}, retained={retained / 1024 / 1024} MB");

        Assert.Equal(1, ReadCount(db));
        Assert.Equal(1, SharpCacheEntryCount() - entriesBefore);
        Assert.True(retained < 3 * EstimatedBytesPerCopy(), $"Expected about one cached copy, got {retained} bytes retained.");
    }

    [Fact]
    public async Task GetAgents_with_different_filters_does_not_share_an_entry()
    {
        var (provider, db) = Build();
        var agentService = provider.GetRequiredService<IAgentService>();

        await agentService.GetAgents(new AgentFilter { AgentIds = ["agent-1"] });
        await agentService.GetAgents(new AgentFilter { AgentIds = ["agent-2"] });
        await agentService.GetAgents(new AgentFilter { AgentIds = ["agent-1"] });

        Assert.Equal(2, ReadCount(db));
    }

    /// <summary>
    /// Joined field by field with "_", these two read the same ("[a]_[b]_[c]_z"), because text can move
    /// between adjacent free-text fields. The content hash keeps them apart.
    /// </summary>
    [Fact]
    public async Task GetAgents_filters_whose_fields_would_splice_into_the_same_text_stay_separate()
    {
        var (provider, db) = Build();
        var agentService = provider.GetRequiredService<IAgentService>();

        await agentService.GetAgents(new AgentFilter { AgentIds = ["a"], AgentNames = ["b"], SimilarName = "[c]_z" });
        await agentService.GetAgents(new AgentFilter { AgentIds = ["a]_[b"], AgentNames = ["c"], SimilarName = "z" });

        Assert.Equal(2, ReadCount(db));
    }

    [Fact]
    public async Task RuleEngine_Triggered_once_per_work_order_reads_the_agents_once()
    {
        var (provider, db) = Build();
        var engine = new RuleEngine(provider, NullLogger<RuleEngine>.Instance);

        var entriesBefore = SharpCacheEntryCount();
        for (var wo = 0; wo < Calls; wo++)
        {
            await engine.Triggered(new StubTrigger { EntityId = wo.ToString() }, $"Work order {wo} has been on site for over 6 hours.");
        }

        _output.WriteLine($"[RuleEngine x {Calls} work orders] repo reads={ReadCount(db)}, cache entries added={SharpCacheEntryCount() - entriesBefore}");
        Assert.Equal(1, ReadCount(db));
        Assert.Equal(1, SharpCacheEntryCount() - entriesBefore);
    }

    /// <summary>
    /// GetAgent is <c>perInstanceCache</c>: callers mutate the agent it returns, so each scope keeps its own
    /// copy. That copy now lives on the scoped AgentService instead of in the shared cache.
    /// </summary>
    [Fact]
    public async Task GetAgent_caches_per_scope_without_adding_shared_cache_entries()
    {
        var (provider, db) = Build();
        const int Scopes = 20;

        var entriesBefore = SharpCacheEntryCount();
        for (var i = 0; i < Scopes; i++)
        {
            using var scope = provider.CreateScope();
            var agentService = scope.ServiceProvider.GetRequiredService<IAgentService>();
            var first = await agentService.GetAgent("agent-0");
            var second = await agentService.GetAgent("agent-0");
            Assert.Same(first, second);
        }

        Assert.Equal(Scopes, db.Invocations.Count(x => x.Method.Name == nameof(IBotSharpRepository.GetAgent)));
        Assert.Equal(0, SharpCacheEntryCount() - entriesBefore);
    }

    /// <summary>
    /// The point of moving per-instance entries out of the shared cache: once the scope is gone, nothing
    /// holds its cached agent any more, instead of it lingering for the full 10 minutes.
    /// </summary>
    [Fact]
    public async Task GetAgent_entry_is_collected_with_its_scope()
    {
        var (provider, db) = Build();

        var (serviceRef, agentRef) = await LoadInDisposedScope(provider);
        // Moq records every return value; drop those records so they do not keep the agent alive.
        db.Invocations.Clear();
        RetainedBytes();

        Assert.False(serviceRef.IsAlive, "The scoped AgentService should be collectable once its scope is disposed.");
        Assert.False(agentRef.IsAlive, "The per-instance cache entry should be collected together with its instance.");
    }

    [Fact]
    public async Task ClearCache_also_clears_per_instance_entries()
    {
        var (provider, db) = Build();
        using var scope = provider.CreateScope();
        var agentService = scope.ServiceProvider.GetRequiredService<IAgentService>();

        await agentService.GetAgent("agent-0");
        // What CreateAgent/UpdateAgent call after changing an agent.
        Utilities.ClearCache();
        await agentService.GetAgent("agent-0");

        Assert.Equal(2, db.Invocations.Count(x => x.Method.Name == nameof(IBotSharpRepository.GetAgent)));
    }

    // Kept out of the test method so no local in the caller's frame roots the scope or the agent.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<(WeakReference service, WeakReference agent)> LoadInDisposedScope(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var agentService = scope.ServiceProvider.GetRequiredService<IAgentService>();
        var agent = await agentService.GetAgent("agent-0");
        return (new WeakReference(agentService), new WeakReference(agent));
    }

    [Fact]
    public async Task GetAgent_does_not_mark_the_shared_default_llm_config_as_inherited()
    {
        var (provider, db) = Build();
        db.Setup(x => x.GetAgent(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((string id, bool _) => { var agent = BuildAgent(id); agent.LlmConfig = null!; return agent; });

        using var scope = provider.CreateScope();
        var agent = await scope.ServiceProvider.GetRequiredService<IAgentService>().GetAgent("agent-0");

        var settings = provider.GetRequiredService<AgentSettings>();
        Assert.True(agent.LlmConfig.IsInherit);
        Assert.False(settings.LlmConfig.IsInherit);
        Assert.NotSame(settings.LlmConfig, agent.LlmConfig);
    }

    [Fact]
    public void Equal_lists_produce_the_same_key()
        => Assert.Equal(KeyOf(new List<int> { 1, 2, 3 }), KeyOf(new List<int> { 1, 2, 3 }));

    [Fact]
    public void Different_lists_produce_different_keys()
        => Assert.NotEqual(KeyOf(new List<int> { 1, 2, 3 }), KeyOf(new List<int> { 1, 2, 4 }));

    [Fact]
    public void Strings_are_length_prefixed_so_joined_arguments_cannot_collide()
        => Assert.NotEqual(KeyOf("a_b") + "_" + KeyOf("c"), KeyOf("a") + "_" + KeyOf("b_c"));

    [Fact]
    public void Long_strings_are_hashed_into_a_bounded_key()
    {
        var key = KeyOf(new string('x', 10_000));
        Assert.True(key!.Length < 100);
        Assert.NotEqual(key, KeyOf(new string('x', 9_999) + "y"));
    }

    [Fact]
    public void An_argument_that_cannot_be_serialized_has_no_key()
    {
        var cyclic = new Node();
        cyclic.Next = cyclic;
        Assert.Null(KeyOf(cyclic));
    }

    /// <summary>
    /// OneBrain loads both CsvNERDataLoader and OneDbEntityDataLoader, each with a cached, argument-less
    /// LoadVocabularyAsync(). Keyed by method name alone, whichever ran first served its vocabulary to both.
    /// </summary>
    [Fact]
    public async Task Same_named_methods_on_different_classes_do_not_share_an_entry()
    {
        Build();
        CsvVocabularyLoader.Loads = 0;

        var first = await new CsvVocabularyLoader().LoadVocabularyAsync();
        var second = await new DbVocabularyLoader().LoadVocabularyAsync();
        await new CsvVocabularyLoader().LoadVocabularyAsync();

        Assert.Equal("csv", first);
        Assert.Equal("db", second);
        // Proves the test classes are actually woven: without SharpCache the second csv call would load again.
        Assert.Equal(1, CsvVocabularyLoader.Loads);
    }

    public class CsvVocabularyLoader
    {
        public static int Loads;

        [SharpCache(60)]
        public async Task<string> LoadVocabularyAsync()
        {
            Interlocked.Increment(ref Loads);
            return await Task.FromResult("csv");
        }
    }

    public class DbVocabularyLoader
    {
        [SharpCache(60)]
        public async Task<string> LoadVocabularyAsync() => await Task.FromResult("db");
    }

    private sealed class Node
    {
        public Node? Next { get; set; }
    }

    private sealed class StubTrigger : IRuleTrigger
    {
        public string EntityType { get; set; } = "client_WOReactive";
        public string EntityId { get; set; } = "0";
        public string Name => "OnSiteOverstayRuleTrigger";
        public string Channel => "crontab";
    }

    /// <summary>The key built for one argument, or null when the call would bypass the cache.</summary>
    private static string? KeyOf(object? arg)
    {
        var method = typeof(SharpCacheAttribute).GetMethod("TryGetCacheKeyByArg", BindingFlags.NonPublic | BindingFlags.Static)!;
        var args = new object?[] { arg, null };
        return (bool)method.Invoke(null, args)! ? (string)args[1]! : null;
    }

    private static (ServiceProvider provider, Mock<IBotSharpRepository> db) Build()
    {
        var db = new Mock<IBotSharpRepository>();
        // A fresh list per call, as the Mongo driver would deserialize it.
        db.Setup(x => x.GetAgents(It.IsAny<AgentFilter>())).ReturnsAsync(() => BuildAgents());
        db.Setup(x => x.GetAgent(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((string id, bool _) => BuildAgent(id));
        db.Setup(x => x.GetPluginConfig()).ReturnsAsync(new PluginConfig());

        var services = new ServiceCollection();
        services.AddSingleton(db.Object);
        services.AddSingleton(new SharpCacheSettings { Enabled = true, Prefix = "test" });
        services.AddSingleton<ICacheService, MemoryCacheService>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton(new AgentSettings());
        services.AddSingleton(new RoutingSettings());
        services.AddSingleton(new BotSharpOptions());
        services.AddSingleton(new PluginLoader(new ServiceCollection(), new ConfigurationBuilder().Build(), new PluginSettings()));
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        // Scoped, as in BotSharp's own registration - this is what makes perInstanceCache per scope.
        services.AddScoped<IAgentService, AgentService>();

        var provider = services.BuildServiceProvider();
        SharpCacheAttribute.Services = provider;
        return (provider, db);
    }

    private static List<Agent> BuildAgents()
        => Enumerable.Range(0, AgentCount).Select(i => BuildAgent($"agent-{i}")).ToList();

    private static Agent BuildAgent(string id) => new()
    {
        Id = id,
        Name = id,
        Disabled = false,
        Instruction = new string('i', InstructionChars),
        Templates =
        [
            new AgentTemplate("t1", new string('a', TemplateChars)),
            new AgentTemplate("t2", new string('b', TemplateChars))
        ],
        Rules = []
    };

    // UTF-16 strings dominate; object overhead ignored, so this is a lower bound.
    private static long EstimatedBytesPerCopy() => (long)AgentCount * (InstructionChars + 2 * TemplateChars) * sizeof(char);

    private static int ReadCount(Mock<IBotSharpRepository> db)
        => db.Invocations.Count(x => x.Method.Name == nameof(IBotSharpRepository.GetAgents));

    private static long RetainedBytes()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        return GC.GetTotalMemory(forceFullCollection: true);
    }

    private static MemoryCache SharpCache()
        => (MemoryCache)typeof(MemoryCacheService)
            .GetField("_cache", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;

    private static int SharpCacheEntryCount() => SharpCache().Count;

    private static void ClearSharpCache() => SharpCache().Compact(1.0);
}
