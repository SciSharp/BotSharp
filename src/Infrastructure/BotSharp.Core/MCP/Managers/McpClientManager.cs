using BotSharp.Core.MCP.Settings;
using ModelContextProtocol.Client;

namespace BotSharp.Core.MCP.Managers;

public class McpClientManager : IDisposable
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

    public async Task<McpClient?> GetMcpClientAsync(string serverId)
    {
        try
        {
            var settings = _services.GetRequiredService<McpSettings>();
            var config = settings.McpServerConfigs.Where(x => x.Id == serverId).FirstOrDefault();
            if (config == null || !config.Enabled)
            {
                return null;
            }

            IClientTransport? transport = null;
            if (config.HttpConfig != null)
            {
                transport = new HttpClientTransport(new HttpClientTransportOptions
                {
                    Name = config.Name,
                    Endpoint = new Uri(config.HttpConfig.EndPoint),
                    AdditionalHeaders = ResolveHeaders(config.Id, config.HttpConfig.AdditionalHeaders),
                    ConnectionTimeout = config.HttpConfig.ConnectionTimeout
                });
            }
            else if (config.SseConfig != null)
            {
                transport = new HttpClientTransport(new HttpClientTransportOptions
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
    /// The headers to open a connection with: the ones from configuration, unless the host has
    /// registered an <see cref="IMcpClientHeaderProvider"/> that wants to adjust them.
    /// </summary>
    /// <remarks>
    /// No provider is registered by default, and a provider is free to answer with what it was
    /// given, so a host without one — or with one that does not recognise this server — gets the
    /// configured headers back untouched.
    /// </remarks>
    private Dictionary<string, string>? ResolveHeaders(string serverId, Dictionary<string, string>? configured)
    {
        var provider = _services.GetService<IMcpClientHeaderProvider>();
        return provider == null ? configured : provider.GetHeaders(serverId, configured);
    }

    public void Dispose()
    {

    }
}
