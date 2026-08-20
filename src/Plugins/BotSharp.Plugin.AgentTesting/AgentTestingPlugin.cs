using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Repositories.Settings;
using BotSharp.Abstraction.Users.Enums;
using BotSharp.Plugin.AgentTesting.Repositories;
using BotSharp.Plugin.AgentTesting.Runtime;
using BotSharp.Plugin.AgentTesting.Services;

namespace BotSharp.Plugin.AgentTesting;

public class AgentTestingPlugin : IBotSharpPlugin
{
    public string Id => "5c1f4d38-9a2e-4b7c-8f61-2d0e7a9c4b13";
    public string Name => "Agent Testing";
    public string Description => "Per-agent regression test sets: scripted multi-turn cases, mocked tools, deterministic assertions.";

    /// <summary>
    /// Without this the four pages exist but nothing links to them -- the suite list is only
    /// reachable by typing the URL. Same shape as QaAutomationPlugin: sit under the "One Brain"
    /// header when it is there, fall back to "Apps" (seeded by BotSharp.OpenAPI's
    /// PluginController), and position with the section's own weight.
    ///
    /// Roles matches the gate that actually matters here: TriggerRun and RecordCase carry
    /// [BotSharpAuth], which is Root/Admin only. The read endpoints are plain [Authorize], but a
    /// menu entry leading to a page whose primary buttons 401 is worse than no entry.
    /// </summary>
    public bool AttachMenu(List<PluginMenuDef> menu)
    {
        var section = menu.FirstOrDefault(x => x.Label == "One Brain")
            ?? menu.FirstOrDefault(x => x.Label == "Apps");

        if (section != null)
        {
            menu.Add(new PluginMenuDef("Agent Testing", icon: "bx bx-test-tube", link: "page/agent-test", weight: section.Weight + 2)
            {
                Roles = new List<string> { UserRole.Root, UserRole.Admin }
            });
        }

        return true;
    }

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        // Singleton: the test context has to be visible across requests and across threads.
        services.AddSingleton<IAgentTestRunRegistry, AgentTestRunRegistry>();

        // Lets BotSharp's own rate limiting recognise a harness conversation and stand aside. Backed
        // by the registry rather than by the conversation's "test-set" tag on purpose: the tag is
        // only written once the conversation row exists, which is after the first message has already
        // been through the rate limit hook, so a tag-based check would still block the first turn of
        // every case. The registry entry exists before the conversation is opened at all.
        services.AddSingleton<ISyntheticConversationProbe, AgentTestSyntheticConversationProbe>();

        // The seam that takes over function execution. Lose this line and mocking silently stops
        // working -- the runner's canary self-check is what catches that.
        services.AddScoped<IFunctionExecutorProvider, TestMockExecutorProvider>();

        // The model-override seam, which lets one run sweep the same agent across several models.
        // Losing this line does not raise an error: every model would run on the agent's own
        // LlmConfig and the comparison grid would show them all behaving identically.
        services.AddScoped<IAgentHook, AgentTestModelOverrideHook>();

        services.AddScoped<IAgentConversationDriver, BotSharpAgentConversationDriver>();

        // Registered by its ICaseRunner interface rather than its concrete type, so that
        // AgentTestRunQueue can substitute a wrapper that opens a fresh DI scope per call in
        // production (see AgentTestRunQueue.ScopedCaseRunner). The behaviour itself -- multi-turn,
        // canary, timeout -- is unchanged by that substitution.
        services.AddScoped<ICaseRunner, AgentTestCaseRunner>();

        // The harness owns its four collections, so it also owns the Mongo connection to them.
        // Singleton because MongoClient is thread-safe and holds a connection pool -- one per DI
        // scope would open a fresh pool for every case a run executes. Bound here rather than taken
        // from the container because BotSharpDatabaseSettings is not registered as a service on
        // every host configuration, while `config` always is.
        var dbSettings = new BotSharpDatabaseSettings();
        config.Bind("Database", dbSettings);
        services.AddSingleton(new AgentTestMongoDbContext(dbSettings));

        services.AddScoped<IAgentTestRepository, AgentTestRepository>();

        // Task 9: no custom interface -- injected by its own concrete type into
        // AgentTestController, so it needs an explicit registration the same way every other
        // service in this method does (there is no auto-discovery mechanism in this plugin).
        services.AddScoped<ICaseSegmenter, LlmCaseSegmenter>();

        // Scoped, like the segmenter: it resolves IChatCompletion implementations out of the same
        // scope and holds no state between calls.
        services.AddScoped<IAgentTestJudge, LlmAgentTestJudge>();
        services.AddScoped<AgentTestRecorder>();

        // AgentTestRunQueue is both a singleton and a BackgroundService: all three lines point at
        // the same instance, sharing one Channel. Same shape as WeChatBackgroundService elsewhere in
        // this repo -- AddSingleton<T>(), AddHostedService forwarding to that same instance, then
        // the interface type forwarding to it as well.
        services.AddSingleton<AgentTestRunQueue>();
        services.AddHostedService(s => s.GetRequiredService<AgentTestRunQueue>());
        services.AddSingleton<IAgentTestRunQueue>(s => s.GetRequiredService<AgentTestRunQueue>());
    }
}
