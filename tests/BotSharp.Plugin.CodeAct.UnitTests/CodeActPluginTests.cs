namespace BotSharp.Plugin.CodeAct.UnitTests;

public class CodeActPluginTests
{
    [Fact]
    public void RegisterDI_Registers_CodeActServices()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton(new AgentSettings());
        services.AddLogging();

        new CodeActPlugin().RegisterDI(services, config);

        var provider = services.BuildServiceProvider().CreateScope().ServiceProvider;

        Assert.NotNull(provider.GetRequiredService<CodeActSettings>());
        Assert.NotNull(provider.GetRequiredService<ICodeActRuntime>());
        Assert.Contains(provider.GetServices<IFunctionCallback>(), x => x.Name == "execute_code");
        Assert.Contains(provider.GetServices<IAgentHook>(), x => x is CodeActAgentHook);
        Assert.NotNull(provider.GetRequiredService<ICodeActSecurityPolicy>());
        Assert.NotNull(provider.GetRequiredService<ICodeActTokenService>());
    }
}
