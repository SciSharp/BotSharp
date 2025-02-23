using McpDotNet.Client;
using McpDotNet.Configuration;

namespace BotSharp.Core.Mcp.Settings;

public class MCPSettings
{
    public McpClientOptions McpClientOptions { get; set; }

    public List<McpServerConfig> McpServerConfigs { get; set; }

}
