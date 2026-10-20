using BotSharp.Core.MCP;
using BotSharp.Core.MCP.Managers;
using BotSharp.Core.MCP.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BotSharp.Core.UnitTests.MCP;

public class SemExampleTests
{
    private static IConfigurationRoot Configuration() => new ConfigurationBuilder()
        .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "sem.json"))
        .Build();

    [Fact]
    public void Configuration_selects_stdio_and_an_explicit_checkout()
    {
        var services = new ServiceCollection().AddLogging();
        services.AddBotSharpMCP(Configuration());
        using var provider = services.BuildServiceProvider();
        var settings = provider.GetRequiredService<McpSettings>();
        Assert.True(settings.Enabled);
        var server = Assert.Single(settings.McpServerConfigs);
        Assert.Equal("sem", server.Id);
        Assert.Null(server.HttpConfig);
        Assert.Equal("sem", server.StdioConfig!.Command);
        Assert.Equal(new[] { "mcp" }, server.StdioConfig.Arguments);
        Assert.Equal("/absolute/path/to/checkout", server.StdioConfig.EnvironmentVariables!["SEM_REPO"]);
    }

    [Fact]
    public async Task Optional_live_lookup_uses_the_configured_stdio_client()
    {
        // Opt in with a disposable checkout containing a parse_config symbol.
        // Normal CI does not need Sem installed or access to a user's checkout.
        var repo = Environment.GetEnvironmentVariable("BOTSHARP_SEM_TEST_REPO");
        if (string.IsNullOrEmpty(repo)) return;
        Assert.True(Path.IsPathRooted(repo) && Directory.Exists(repo));
        var configuration = Configuration();
        configuration["MCP:McpServerConfigs:0:StdioConfig:EnvironmentVariables:SEM_REPO"] = repo;
        var services = new ServiceCollection().AddLogging();
        services.AddBotSharpMCP(configuration);
        await using var provider = services.BuildServiceProvider();
        await using var client = await provider.GetRequiredService<McpClientManager>().GetMcpClientAsync("sem");
        Assert.NotNull(client);
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var tools = await client.ListToolsAsync(cancellationToken: timeout.Token);
        Assert.Contains(tools, tool => tool.Name == "sem_find");
        var result = await client.CallToolAsync("sem_find", new Dictionary<string, object?>
        {
            ["query"] = "parse_config", ["mode"] = "context", ["token_budget"] = 1200
        }, cancellationToken: timeout.Token);
        Assert.NotEqual(true, result.IsError);
        Assert.Contains("parse_config", System.Text.Json.JsonSerializer.Serialize(result));
    }
}
