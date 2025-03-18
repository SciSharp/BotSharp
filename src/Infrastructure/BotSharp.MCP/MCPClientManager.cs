using BotSharp.Core.Mcp.Settings;
using McpDotNet.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BotSharp.Core.Mcp;

public class MCPClientManager : IDisposable
{
    public ILoggerFactory LoggerFactory { get; }
 
    private readonly MCPSettings mcpSettings;

    public MCPClientManager(MCPSettings settings, ILoggerFactory loggerFactory)
    {
        mcpSettings = settings;
        LoggerFactory = loggerFactory;             
    }

    public async Task<IMcpClient> GetMcpClientAsync(string serverId)
    {
        return await McpClientFactory.CreateAsync(mcpSettings.McpServerConfigs
            .Where(x=> x.Name == serverId).First(), mcpSettings.McpClientOptions);
    }

    public void Dispose()
    {
        LoggerFactory?.Dispose();
    }
}
