using ModelContextProtocol.Client;

namespace BotSharp.Core.MCP.Settings;

public class McpSettings
{
    public bool Enabled { get; set; } = true;
    public McpClientOptions McpClientOptions { get; set; }
    public List<McpServerConfigModel> McpServerConfigs { get; set; } = [];

}
