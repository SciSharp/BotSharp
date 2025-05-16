using BotSharp.Core.MCP.Settings;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;

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

    public async Task<IMcpClient?> GetMcpClientAsync(string serverId)
    {
        try
        {
            var settings = _services.GetRequiredService<McpSettings>();
            var config = settings.McpServerConfigs.Where(x => x.Id == serverId).FirstOrDefault();
            if (config == null)
            {
                return null;
            }

            IClientTransport? transport = null;
            if (config.SseConfig != null)
            {
                transport = new SseClientTransport(new SseClientTransportOptions
                {
                    Name = config.Name,
                    Endpoint = new Uri(config.SseConfig.EndPoint),
                    AdditionalHeaders = config.SseConfig.AdditionalHeaders,
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

            return await McpClientFactory.CreateAsync(transport, settings.McpClientOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error when loading mcp client {serverId}");
            return null;
        }
    }

    public void Dispose()
    {

    }
}
