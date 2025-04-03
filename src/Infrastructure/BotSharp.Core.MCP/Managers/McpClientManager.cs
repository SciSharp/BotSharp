using BotSharp.Core.MCP.Settings;
using ModelContextProtocol.Client;

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
        return await McpClientFactory.CreateAsync(
            _mcpSettings.McpServerConfigs.Where(x=> x.Name == serverId).First(),
            _mcpSettings.McpClientOptions);
    }

    public void Dispose()
    {

    }
}
