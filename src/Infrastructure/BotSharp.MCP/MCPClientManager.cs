using BotSharp.Core.Mcp.Settings;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BotSharp.Core.Mcp;

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
