using System.Net;
using System.Text;
using System.Text.Json;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Infrastructures;
using BotSharp.Abstraction.MCP.Services;
using BotSharp.Abstraction.MessageHub.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Core.Infrastructures;
using BotSharp.Core.MCP;
using BotSharp.Core.MCP.Hooks;
using BotSharp.Core.MCP.Managers;
using BotSharp.Core.MCP.Settings;
using BotSharp.Core.MessageHub;
using BotSharp.Core.Routing.Executor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace BotSharp.Core.UnitTests.MCP;

public class ParallelSearchExampleTests(ITestOutputHelper output)
{
    private const string ServerId = "parallel-search";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Empty_or_disabled_configuration_does_not_register_a_client(bool enabled)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MCP:Enabled"] = enabled.ToString()
        }).Build();
        var services = new ServiceCollection().AddLogging();
        services.AddBotSharpMCP(config);
        using var provider = services.BuildServiceProvider();
        Assert.Null(provider.GetService<McpClientManager>());
        Assert.Empty(provider.GetRequiredService<McpSettings>().McpServerConfigs);
    }

    [Fact]
    public async Task Configuration_loads_agent_tools_and_executes_search_and_fetch()
    {
        // Only this explicit switch sends requests to the public service. CI uses a local fixture.
        var live = Environment.GetEnvironmentVariable("BOTSHARP_PARALLEL_LIVE") == "1";
        var config = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "parallel-search.json"))
            .Build();
        var services = new ServiceCollection();
        services.AddLogging(x => x.AddConsole());
        services.AddBotSharpMCP(config);
        services.AddSingleton<ICacheService, MemoryCacheService>();
        var state = new Mock<IConversationStateService>();
        var conversation = new Mock<IConversationService>();
        conversation.Setup(x => x.IsConversationMode()).Returns(true);
        conversation.SetupGet(x => x.States).Returns(state.Object);
        services.AddSingleton(conversation.Object);
        services.AddSingleton<MessageHub<HubObserveData<RoleDialogModel>>>();
        services.AddSingleton(new AgentSettings());
        var requests = new List<string>();
        services.AddHttpClient($"mcp:{ServerId}")
            .AddHttpMessageHandler(() => new RecordingHandler(requests, live));
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var settings = sp.GetRequiredService<McpSettings>();
        Assert.True(settings.Enabled);
        var server = Assert.Single(settings.McpServerConfigs);
        Assert.Equal("https://search.parallel.ai/mcp", server.HttpConfig!.EndPoint);
        Assert.Null(server.HttpConfig.AdditionalHeaders!.GetValueOrDefault("Authorization"));
        output.WriteLine($"Mode: {(live ? "live keyless HTTP" : "local HTTP fixture")}");

        var options = await sp.GetRequiredService<IMcpService>().GetServerConfigsAsync();
        Assert.Contains("web_search", Assert.Single(options).Tools);
        Assert.Contains("web_fetch", Assert.Single(options).Tools);
        var agent = new Agent
        {
            McpTools = [new McpTool("Parallel Search", ServerId, functions:
                [new McpFunction("web_search"), new McpFunction("web_fetch")])]
        };
        var hook = new McpToolAgentHook(sp, sp.GetRequiredService<AgentSettings>());
        await hook.OnAgentMcpToolLoaded(agent);
        Assert.Contains(agent.SecondaryFunctions, x => x.Name == "web_search");
        Assert.Contains(agent.SecondaryFunctions, x => x.Name == "web_fetch");

        var sessionId = Guid.NewGuid().ToString();
        var factory = new FunctionExecutorFactory(sp);
        var search = new RoleDialogModel
        {
            FunctionName = "web_search",
            FunctionArgs = JsonSerializer.Serialize(new
            {
                objective = "Find Microsoft's official documentation explaining what .NET is.",
                search_queries = new[] { "site:learn.microsoft.com dotnet introduction" },
                session_id = sessionId
            })
        };
        var searchExecutor = Assert.IsType<McpToolExecutor>(factory.Create(search.FunctionName, agent));
        Assert.True(await searchExecutor.ExecuteAsync(search), search.Content);
        Assert.Contains("https://", search.Content);
        Assert.Contains(".NET", search.Content, StringComparison.OrdinalIgnoreCase);
        output.WriteLine("Search output: " + search.Content);

        // Fetch a specific page for exact documentation, rather than fetching every search result.
        var fetch = new RoleDialogModel
        {
            FunctionName = "web_fetch",
            FunctionArgs = JsonSerializer.Serialize(new
            {
                urls = new[] { "https://learn.microsoft.com/en-us/dotnet/core/introduction" },
                objective = "Explain what .NET is using the official introduction.",
                session_id = sessionId
            })
        };
        var fetchExecutor = Assert.IsType<McpToolExecutor>(factory.Create(fetch.FunctionName, agent));
        Assert.True(await fetchExecutor.ExecuteAsync(fetch), fetch.Content);
        Assert.Contains(".NET", fetch.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("https://learn.microsoft.com", fetch.Content);
        output.WriteLine("Fetch output: " + fetch.Content);
        Assert.Contains(requests, x => x == "tools/list");
        Assert.Contains(requests, x => x == "tools/call:web_search");
        Assert.Contains(requests, x => x == "tools/call:web_fetch");
        output.WriteLine("Observed requests with BotSharp User-Agent and no auth: " + string.Join(", ", requests));

        agent.McpTools[0].Disabled = true;
        agent.SecondaryFunctions.Clear();
        var count = requests.Count;
        await hook.OnAgentMcpToolLoaded(agent);
        Assert.Empty(agent.SecondaryFunctions);
        Assert.Equal(count, requests.Count);
        server.Enabled = false;
        Assert.Null(await sp.GetRequiredService<McpClientManager>().GetMcpClientAsync(ServerId));
        Assert.Equal(count, requests.Count);
    }

    private sealed class RecordingHandler(List<string> requests, bool live) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Contains("BotSharp/5.2.0", request.Headers.UserAgent.ToString());
            Assert.False(request.Headers.Contains("Authorization"));
            Assert.False(request.Headers.Contains("x-api-key"));
            if (request.Method != HttpMethod.Post)
                return live ? await base.SendAsync(request, cancellationToken) : new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);

            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var rpc = json.RootElement;
            var method = rpc.GetProperty("method").GetString();
            var tool = method == "tools/call" ? rpc.GetProperty("params").GetProperty("name").GetString() : null;
            if (tool != null)
            {
                var args = rpc.GetProperty("params").GetProperty("arguments");
                Assert.True(Guid.TryParse(args.GetProperty("session_id").GetString(), out _));
                Assert.False(string.IsNullOrWhiteSpace(args.GetProperty("objective").GetString()));
                Assert.Equal(JsonValueKind.Array, args.GetProperty(tool == "web_search" ? "search_queries" : "urls").ValueKind);
            }
            requests.Add(tool == null ? method! : $"{method}:{tool}");
            if (live) return await base.SendAsync(request, cancellationToken);
            if (!rpc.TryGetProperty("id", out var id)) return new HttpResponseMessage(HttpStatusCode.Accepted);
            object result = method switch
            {
                "initialize" => new { protocolVersion = "2025-03-26", capabilities = new { tools = new { } }, serverInfo = new { name = "Local search fixture", version = "1.0.0" } },
                "tools/list" => new { tools = new[] { Tool("web_search"), Tool("web_fetch") } },
                "tools/call" => new { content = new[] { new { type = "text", text = ".NET is a free, cross-platform developer platform. Source: https://learn.microsoft.com/en-us/dotnet/core/introduction" } }, isError = false },
                _ => throw new InvalidOperationException("Unexpected RPC method: " + method)
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { jsonrpc = "2.0", id, result }), Encoding.UTF8, "application/json")
            };
        }

        private static object Tool(string name) => new
        {
            name,
            description = "Local fixture for " + name,
            inputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        };
    }
}
