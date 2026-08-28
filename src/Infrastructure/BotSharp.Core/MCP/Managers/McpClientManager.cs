using System.Collections.Concurrent;
using System.Security.Cryptography;
using BotSharp.Core.MCP.Settings;
using ModelContextProtocol.Client;

namespace BotSharp.Core.MCP.Managers;

/// <summary>
/// Hands out MCP clients, pooled for the lifetime of the DI scope that resolved this manager.
/// </summary>
/// <remarks>
/// <para>
/// WHY THE POOL IS SCOPED, AND HAS TO STAY SCOPED. A connection carries whatever
/// <see cref="IMcpClientHeaderProvider"/> answered with, which is how a host calls a server as
/// the signed-in user instead of with one shared credential. Reusing a connection therefore
/// reuses an identity. This class is registered per scope — one HTTP request, one crontab run,
/// one queued message — so everything sharing a pool is already the same caller, and one user's
/// connection cannot be handed to another. That is structural, not a rule someone has to
/// remember.
/// </para>
/// <para>
/// The pool key carries a fingerprint of the headers a connection opens with, so the guarantee
/// survives this class later being registered with a longer lifetime: two credentials land on
/// two entries even inside one pool. The fingerprint is a hash of secrets and is never logged.
/// </para>
/// <para>
/// Before pooling, every tool call opened its own connection and none of them were ever closed:
/// this method built a fresh transport per call and <see cref="Dispose"/> did nothing. A turn
/// that listed tools and then called three of them opened four connections and leaked all four.
/// </para>
/// </remarks>
public class McpClientManager : IDisposable, IAsyncDisposable
{
    private const string KeySeparator = "|";

    /// <summary>
    /// How long a synchronous scope teardown waits for connections to close before giving up.
    /// </summary>
    private static readonly TimeSpan SyncCloseTimeout = TimeSpan.FromSeconds(5);

    private readonly IServiceProvider _services;
    private readonly ILogger<McpClientManager> _logger;
    private readonly ConcurrentDictionary<string, PooledClient> _pool = new();
    private volatile bool _disposed;

    public McpClientManager(
        IServiceProvider services,
        ILogger<McpClientManager> logger)
    {
        _services = services;
        _logger = logger;
    }

    /// <summary>
    /// The client for <paramref name="serverId"/>, opening one if this scope has not already.
    /// Answers null rather than throwing when the server is unknown, disabled or unreachable,
    /// which is the contract every caller is already written against.
    /// </summary>
    public async Task<McpClient?> GetMcpClientAsync(string serverId)
    {
        if (_disposed)
        {
            return null;
        }

        McpServerConfigModel config;
        Dictionary<string, string>? headers;
        string key;

        try
        {
            var settings = _services.GetRequiredService<McpSettings>();
            var found = settings.McpServerConfigs?.FirstOrDefault(x => x.Id == serverId);
            if (found == null || !found.Enabled)
            {
                return null;
            }

            config = found;

            // Resolved once, here, and handed to the transport below. Resolving separately for
            // the key and for the connection would let the two disagree, and the key is the
            // thing keeping one caller's connection away from another.
            headers = ResolveHeaders(config);
            key = BuildPoolKey(serverId, headers);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error when loading mcp client {serverId}");
            return null;
        }

        // Lazy rather than a bare Task, so parallel tool calls that all want this server open
        // one connection between them instead of one each.
        var entry = _pool.GetOrAdd(key, _ => new PooledClient(serverId, new Lazy<Task<McpClient?>>(
            () => CreateClientAsync(config, headers),
            LazyThreadSafetyMode.ExecutionAndPublication)));

        McpClient? client = null;
        try
        {
            client = await entry.Client.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error when loading mcp client {serverId}");
        }

        if (client == null)
        {
            // A failure must not stay cached, or every later call in this scope gets it back.
            // Removed by value, so a retry that already replaced the entry survives.
            _pool.TryRemove(new KeyValuePair<string, PooledClient>(key, entry));
        }

        return client;
    }

    /// <summary>
    /// Drops and closes this scope's connection to <paramref name="serverId"/> so the next call
    /// opens a fresh one. Call it when a request over that connection failed at the transport
    /// level: keeping a dead client fails every remaining call in the turn, while discarding a
    /// live one costs a single reconnect, and that asymmetry says always discard.
    /// </summary>
    public async Task InvalidateAsync(string serverId)
    {
        var prefix = serverId + KeySeparator;
        foreach (var key in _pool.Keys.Where(x => x.StartsWith(prefix, StringComparison.Ordinal)).ToList())
        {
            if (_pool.TryRemove(key, out var entry))
            {
                await CloseAsync(entry);
            }
        }
    }

    private async Task<McpClient?> CreateClientAsync(McpServerConfigModel config, Dictionary<string, string>? headers)
    {
        try
        {
            var settings = _services.GetRequiredService<McpSettings>();

            IClientTransport? transport = null;
            if (config.HttpConfig != null)
            {
                transport = new HttpClientTransport(new HttpClientTransportOptions
                {
                    Name = config.Name,
                    Endpoint = new Uri(config.HttpConfig.EndPoint),
                    AdditionalHeaders = headers,
                    ConnectionTimeout = config.HttpConfig.ConnectionTimeout
                });
            }
            else if (config.SseConfig != null)
            {
                transport = new HttpClientTransport(new HttpClientTransportOptions
                {
                    Name = config.Name,
                    Endpoint = new Uri(config.SseConfig.EndPoint),
                    AdditionalHeaders = headers,
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
            _logger.LogWarning(ex, $"Error when loading mcp client {config.Id}");
            return null;
        }
    }

    /// <summary>
    /// The headers to open a connection with: the ones from configuration, unless the host has
    /// registered an <see cref="IMcpClientHeaderProvider"/> that wants to adjust them.
    /// </summary>
    /// <remarks>
    /// No provider is registered by default, and a provider is free to answer with what it was
    /// given, so a host without one — or with one that does not recognise this server — gets the
    /// configured headers back untouched. A stdio server opens with no connection headers at
    /// all, so the provider is not consulted for one.
    /// </remarks>
    private Dictionary<string, string>? ResolveHeaders(McpServerConfigModel config)
    {
        if (config.HttpConfig == null && config.SseConfig == null)
        {
            return null;
        }

        var configured = config.HttpConfig?.AdditionalHeaders ?? config.SseConfig?.AdditionalHeaders;
        var provider = _services.GetService<IMcpClientHeaderProvider>();
        return provider == null ? configured : provider.GetHeaders(config.Id, configured);
    }

    /// <summary>
    /// The server id plus a fingerprint of the headers the connection will carry, so an entry is
    /// shared only between calls that authenticate identically. Hashed rather than kept, because
    /// those headers hold credentials; the result is treated as a secret and never logged.
    /// </summary>
    internal static string BuildPoolKey(string serverId, Dictionary<string, string>? headers)
    {
        if (headers == null || headers.Count == 0)
        {
            return serverId + KeySeparator;
        }

        var canonical = new StringBuilder();
        foreach (var pair in headers.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            canonical.Append(pair.Key).Append(' ').Append(pair.Value).Append('\n');
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()));
        return serverId + KeySeparator + Convert.ToHexString(hash);
    }

    private async Task CloseAsync(PooledClient entry)
    {
        try
        {
            var client = await entry.Client.Value;
            if (client != null)
            {
                await client.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error when closing mcp client {entry.ServerId}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var pair in _pool.ToArray())
        {
            if (_pool.TryRemove(pair.Key, out var entry))
            {
                await CloseAsync(entry);
            }
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Scopes made with CreateScope — crontab runs, queue consumers — tear down synchronously,
    /// and <see cref="McpClient"/> only offers DisposeAsync, so this waits for it. The wait is
    /// bounded: a wedged transport must not hang the unit of work that is trying to finish. An
    /// async scope, an ASP.NET Core request among them, calls <see cref="DisposeAsync"/> instead
    /// and never comes through here.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            if (!DisposeAsync().AsTask().Wait(SyncCloseTimeout))
            {
                _logger.LogWarning("Timed out closing pooled MCP clients; leaving them to the transport.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error when closing pooled MCP clients.");
        }

        GC.SuppressFinalize(this);
    }

    private sealed record PooledClient(string ServerId, Lazy<Task<McpClient?>> Client);
}
