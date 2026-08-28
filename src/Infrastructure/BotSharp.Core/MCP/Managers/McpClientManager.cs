using BotSharp.Core.MCP.Settings;
using ModelContextProtocol.Client;
using System.Net.Http;

namespace BotSharp.Core.MCP.Managers;

/// <summary>
/// Opens MCP clients. Each call returns a client of its own, which the caller owns and must
/// dispose; what is shared between callers is the HTTP connection underneath it.
/// </summary>
/// <remarks>
/// <para>
/// WHY NOTHING ABOVE THE SOCKET IS SHARED. An MCP client is a session: CreateAsync performs the
/// initialize handshake, the server answers with a session id, and subscriptions and long-running
/// tool tasks (ListTasksAsync, GetTaskResultAsync) live on that session. Handing one session to
/// two callers would show one of them the other's tasks, and no per-request header can undo that
/// because it is server-side state rather than an authorization question. Since
/// <see cref="IMcpClientHeaderProvider"/> lets a host open a connection as the signed-in user,
/// sharing a session would also mean sharing an identity. So sessions are never shared.
/// </para>
/// <para>
/// WHAT IS SHARED. The HttpClient comes from IHttpClientFactory, named per server, so every
/// connection to one server reuses a pooled HttpMessageHandler -- the same TCP and TLS the
/// factory would give any other caller. That layer carries no identity: the credential lives in
/// the transport's headers, and CreateClient hands back a fresh HttpClient each time, so headers
/// set for one caller are never seen by another. This is what makes a per-call session cheap:
/// the handshake runs over an already-warm connection.
/// </para>
/// <para>
/// Building the transport with its own HttpClient, as this did before, gave every MCP connection
/// a private handler and therefore a private socket pool -- the usual way to exhaust sockets and
/// to keep talking to an address DNS has already moved.
/// </para>
/// </remarks>
public class McpClientManager
{
    private readonly IServiceProvider _services;
    private readonly ILogger<McpClientManager> _logger;

    public McpClientManager(
        IServiceProvider services,
        ILogger<McpClientManager> logger)
    {
        _services = services;
        _logger = logger;
    }

    /// <summary>
    /// Opens a client for <paramref name="serverId"/>. <b>The caller owns it and must dispose it</b>
    /// -- an undisposed client leaves its session open on the server until the server times it out.
    /// Answers null rather than throwing when the server is unknown, disabled or unreachable.
    /// </summary>
    public async Task<McpClient?> GetMcpClientAsync(string serverId)
    {
        try
        {
            var settings = _services.GetRequiredService<McpSettings>();
            var config = settings.McpServerConfigs?.FirstOrDefault(x => x.Id == serverId);
            if (config == null || !config.Enabled)
            {
                return null;
            }

            IClientTransport? transport = null;
            if (config.HttpConfig != null)
            {
                transport = CreateHttpTransport(config, new HttpClientTransportOptions
                {
                    Name = config.Name,
                    Endpoint = new Uri(config.HttpConfig.EndPoint),
                    AdditionalHeaders = ResolveHeaders(config.Id, config.HttpConfig.AdditionalHeaders),
                    ConnectionTimeout = config.HttpConfig.ConnectionTimeout
                });
            }
            else if (config.SseConfig != null)
            {
                transport = CreateHttpTransport(config, new HttpClientTransportOptions
                {
                    Name = config.Name,
                    Endpoint = new Uri(config.SseConfig.EndPoint),
                    AdditionalHeaders = ResolveHeaders(config.Id, config.SseConfig.AdditionalHeaders),
                    ConnectionTimeout = config.SseConfig.ConnectionTimeout
                });
            }
            else if (config.StdioConfig != null)
            {
                transport = new StdioClientTransport(new StdioClientTransportOptions
                {
                    Name = config.Name,
                    Command = config.StdioConfig.Command,
                    Arguments = config.StdioConfig.Arguments,
                    EnvironmentVariables = config.StdioConfig.EnvironmentVariables,
                    ShutdownTimeout = config.StdioConfig.ShutdownTimeout
                });
            }

            if (transport == null)
            {
                return null;
            }

            return await McpClient.CreateAsync(transport, settings.McpClientOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error when loading mcp client {serverId}");
            return null;
        }
    }

    /// <summary>
    /// A transport over an HttpClient from the factory, named for this server so its handler --
    /// and therefore its connection pool -- is reused by every later connection to the same
    /// server. The instance itself is fresh per call, which is what keeps one caller's headers
    /// out of another's request.
    /// </summary>
    private HttpClientTransport CreateHttpTransport(McpServerConfigModel config, HttpClientTransportOptions options)
    {
        var factory = _services.GetRequiredService<IHttpClientFactory>();
        var http = factory.CreateClient(HttpClientName(config.Id));

        // Timeout is left at the factory default (100s) deliberately: no configured tool is
        // expected to run that long. Note this is a cap the SDK's own HttpClient may not have
        // had, so it arrived with this change -- a server whose transport keeps a GET open for
        // the session (SSE, or streamable HTTP with a standalone listening stream) would be cut
        // off at 100s no matter how quick its tools are. The symptom is a tool call failing with
        // a canceled request; the fix is Timeout.InfiniteTimeSpan here.

        return new HttpClientTransport(options, http, loggerFactory: null, ownsHttpClient: true);
    }

    /// <summary>
    /// One handler pool per server, so a slow or unhealthy server cannot occupy the connections
    /// of the others.
    /// </summary>
    private static string HttpClientName(string serverId) => $"mcp:{serverId}";

    /// <summary>
    /// The headers to open a connection with: the ones from configuration, unless the host has
    /// registered an <see cref="IMcpClientHeaderProvider"/> that wants to adjust them.
    /// </summary>
    /// <remarks>
    /// No provider is registered by default, and a provider is free to answer with what it was
    /// given, so a host without one -- or with one that does not recognise this server -- gets the
    /// configured headers back untouched.
    /// </remarks>
    private Dictionary<string, string>? ResolveHeaders(string serverId, Dictionary<string, string>? configured)
    {
        var provider = _services.GetService<IMcpClientHeaderProvider>();
        return provider == null ? configured : provider.GetHeaders(serverId, configured);
    }
}
