using ModelContextProtocol.Client;
using ModelContextProtocol.Configuration;

namespace BotSharp.Core.MCP.Settings;

public class McpSettings
{
    public bool Enabled { get; set; } = true;
    public McpClientOptions McpClientOptions { get; set; }
    public List<McpServerConfig> McpServerConfigs { get; set; } = new();

}
