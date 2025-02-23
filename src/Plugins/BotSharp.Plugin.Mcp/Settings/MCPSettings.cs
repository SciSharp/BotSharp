using McpDotNet.Client;
using McpDotNet.Configuration;
using System.Collections.Generic;

namespace BotSharp.Plugin.Mcp.Settings;

public class MCPSettings
{
    public McpClientOptions McpClientOptions { get; set; }

    public List<McpServerConfig> McpServerConfigs { get; set; }

}
