using BotSharp.Core.MCP.Settings;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;

namespace BotSharp.Core.MCP.Managers;

public class McpClientManager : IDisposable
{
    private readonly McpSettings _mcpSettings;

    public McpClientManager(McpSettings mcpSettings)
    {
        _mcpSettings = mcpSettings;
    }

    public async Task<IMcpClient> GetMcpClientAsync(string serverId)
    {
        var config = _mcpSettings.McpServerConfigs.Where(x => x.Id == serverId).FirstOrDefault();

        IClientTransport transport;
        if (config.SseConfig != null)
        {
            transport = new SseClientTransport(new SseClientTransportOptions
            {
                Name = config.Name,
                Endpoint = new Uri(config.SseConfig.EndPoint)
            });
        }
        else if (config.StdioConfig != null)
        {
            transport = new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = config.Name,
                Command = config.StdioConfig.Command,
                Arguments = config.StdioConfig.Arguments,
                EnvironmentVariables = config.StdioConfig.EnvironmentVariables
            });
        }
        else
        {
            throw new ArgumentNullException("Invalid MCP server configuration!");
        }

        return await McpClientFactory.CreateAsync(transport, _mcpSettings.McpClientOptions);
    }

    public void Dispose()
    {

    }
}
