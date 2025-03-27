using ModelContextProtocol.Client;
using ModelContextProtocol.Configuration;
using System.Collections.Generic;

namespace BotSharp.Core.Mcp.Settings;

public class MCPSettings
{
    public McpClientOptions McpClientOptions { get; set; }

    public List<McpServerConfig> McpServerConfigs { get; set; }

}
