namespace BotSharp.Plugin.CodeAct.UnitTests.Runtime;

public class CodeActRuntimeSelectionTests
{
    [Fact]
    public void RegisterDI_SelectsFakeRuntime_ByDefault()
    {
        using var provider = CreateProvider(new Dictionary<string, string?>());

        var runtime = provider.GetRequiredService<ICodeActRuntime>();

        Assert.IsType<FakeCodeActRuntime>(runtime);
    }

    [Fact]
    public void RegisterDI_SelectsOpenSandboxRuntime_WhenConfigured()
    {
        using var provider = CreateProvider(new Dictionary<string, string?>
        {
            ["CodeAct:Runtime"] = "opensandbox"
        });

        var runtime = provider.GetRequiredService<ICodeActRuntime>();

        Assert.IsType<OpenSandboxCodeActRuntime>(runtime);
    }

    [Fact]
    public void RegisterDI_Throws_ForUnknownRuntime()
    {
        using var provider = CreateProvider(new Dictionary<string, string?>
        {
            ["CodeAct:Runtime"] = "unknown"
        });

        var ex = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ICodeActRuntime>());

        Assert.Contains("Unsupported CodeAct runtime", ex.Message);
    }

    private static ServiceProvider CreateProvider(Dictionary<string, string?> values)
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton(new AgentSettings());
        services.AddLogging();

        new CodeActPlugin().RegisterDI(services, config);

        return services.BuildServiceProvider();
    }
}
