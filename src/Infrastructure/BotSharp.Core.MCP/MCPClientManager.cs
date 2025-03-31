using BotSharp.Core.MCP.Settings;
using ModelContextProtocol.Client;

namespace BotSharp.Core.MCP;

public class MCPClientManager : IDisposable
{

    private readonly MCPSettings mcpSettings;

    public MCPClientManager(MCPSettings settings)
    {
        mcpSettings = settings;
    }

    public async Task<IMcpClient> GetMcpClientAsync(string serverId)
    {
        return await McpClientFactory.CreateAsync(mcpSettings.McpServerConfigs
            .Where(x=> x.Name == serverId).First(), mcpSettings.McpClientOptions);
    }

    public void Dispose()
    {

    }
}
