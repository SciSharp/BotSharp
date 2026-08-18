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
        // 单例：测试上下文要跨请求/跨线程可见。
        services.AddSingleton<IAgentTestRunRegistry, AgentTestRunRegistry>();

        // 接管函数执行的接缝。若这一行丢了，mock 会静默失效——运行器的 canary 自检会兜住。
        services.AddScoped<IFunctionExecutorProvider, TestMockExecutorProvider>();

        // The model-override seam, which lets one run sweep the same agent across several models.
        // Losing this line does not raise an error: every model would run on the agent's own
        // LlmConfig and the comparison grid would show them all behaving identically.
        services.AddScoped<IAgentHook, AgentTestModelOverrideHook>();

        services.AddScoped<IAgentConversationDriver, BotSharpAgentConversationDriver>();

        // Task 8: AgentTestCaseRunner 现在按 ICaseRunner 接口注册（而不是原来的具体类型），
        // 好让 AgentTestRunQueue 能在生产环境里把它换成一个"每次调用都开新 DI scope"的包装
        // （见 AgentTestRunQueue.ScopedCaseRunner）——这条替换是本任务里唯一一处修改 Task 7
        // 已有代码的地方，行为本身（多轮 + canary + 超时）完全不变。
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
        services.AddScoped<AgentTestRecorder>();

        // AgentTestRunQueue 既是单例又是 BackgroundService：三行都指向同一个实例（同一份
        // Channel），仿照本仓库里 BotSharp.Plugin.WeChat 的 WeChatBackgroundService 那套写法
        // （services.AddSingleton<T>() + AddHostedService(s => s.GetRequiredService<T>()) +
        // 再按接口类型转发一次）。
        services.AddSingleton<AgentTestRunQueue>();
        services.AddHostedService(s => s.GetRequiredService<AgentTestRunQueue>());
        services.AddSingleton<IAgentTestRunQueue>(s => s.GetRequiredService<AgentTestRunQueue>());
    }
}
