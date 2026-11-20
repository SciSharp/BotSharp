using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Plugin.Membase.GraphDb;
using BotSharp.Plugin.Membase.Handlers;
using BotSharp.Plugin.Membase.Hooks;
using Refit;
using System.Net.Http;
using System.Net.Sockets;

namespace BotSharp.Plugin.Membase;

[PluginDependency("BotSharp.Plugin.KnowledgeBase")]
public class MembasePlugin : IBotSharpPlugin
{
    public string Id => "8df12767-9a44-45d9-93cd-12a10adf3933";
    public string Name => "Membase";
    public string Description => "Document Database with Graph Traversal & Vector Search.";
    public string IconUrl => "https://www.membase.dev/favicon.png";

    private string _membaseCredential = string.Empty;
    private string _membaseProjectId = string.Empty;

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new MembaseSettings();
        config.Bind("Membase", settings);
        services.AddSingleton(sp => settings);

        services.AddHttpContextAccessor();
        services.AddTransient<MembaseAuthHandler>();
        services.AddRefitClient<IMembaseApi>(new RefitSettings
                {
                    CollectionFormat = CollectionFormat.Multi
                })
                .ConfigurePrimaryHttpMessageHandler(CreatePrimaryHandler)
                .AddHttpMessageHandler<MembaseAuthHandler>()
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = new Uri(settings.Host);
                    // MembaseGraphDb bounds each query with Membase:TimeoutSecond; this keeps the HTTP client from
                    // cutting it shorter. Calls made on IMembaseApi directly have no other timeout, so keep a floor.
                    c.Timeout = TimeSpan.FromSeconds(Math.Max(90, settings.TimeoutSecond));
                });

        services.AddScoped<IGraphDb, MembaseGraphDb>();
        services.AddScoped<IAgentUtilityHook, MembaseUtilityHook>();
        services.AddScoped<IInstructionResolver, MembaseInstructionResolver>();

        _membaseCredential = config.GetValue<string>("Membase:ApiKey") ?? string.Empty;
        _membaseProjectId = config.GetValue<string>("Membase:ProjectId") ?? string.Empty;
    }

    /// <summary>
    /// Keeps pooled connections from being reset by NATs, firewalls and proxies between the caller and Membase.
    /// Membase's front end closes connections idle for about two minutes, and middleboxes may drop them sooner,
    /// so idle connections are retired after 30 s instead of being reused once something on the path has
    /// forgotten them. TCP keepalive keeps a connection's flow alive while a long query is still running.
    /// </summary>
    private static SocketsHttpHandler CreatePrimaryHandler() => new()
    {
        PooledConnectionIdleTimeout = TimeSpan.FromSeconds(30),
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        ConnectCallback = async (context, cancellationToken) =>
        {
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 30);
                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 10);
                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 3);
                await socket.ConnectAsync(context.DnsEndPoint, cancellationToken).ConfigureAwait(false);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    };

    public bool AttachMenu(List<PluginMenuDef> menu)
    {
        var section = menu.First(x => x.Label == "Knowledge Base");
        section?.SubMenu?.Add(new PluginMenuDef("Relationships", link: "page/knowledge-base/relationships/membase")
        {
            EmbeddingInfo = new EmbeddingData
            {
                Source = "membase",
                HtmlTag = "iframe",
                Url = $"https://console.membase.dev/query-editor/{_membaseProjectId}?token={_membaseCredential}",
                HtmlStyle = "width: 100%; height: 98%;",
                FullScreen = true
            }
        });
        return true;
    }
}
