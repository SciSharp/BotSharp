using BotSharp.Core.MCP.Settings;
using ModelContextProtocol.Client;

namespace BotSharp.Core.MCP.Managers;

public class McpClientManager : IDisposable
{

    private readonly McpSettings mcpSettings;

    public McpClientManager(McpSettings settings)
    {
        mcpSettings = settings;
    }

    public async Task<IMcpClient> GetMcpClientAsync(string serverId)
    {
        return await McpClientFactory.CreateAsync(
            mcpSettings.McpServerConfigs.Where(x=> x.Name == serverId).First(),
            mcpSettings.McpClientOptions);
    }

    public void Dispose()
    {

    }
}
