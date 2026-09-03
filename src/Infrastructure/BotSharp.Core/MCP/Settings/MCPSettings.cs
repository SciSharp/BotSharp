using ModelContextProtocol.Client;

namespace BotSharp.Core.MCP.Settings;

public class McpSettings
{
    public bool Enabled { get; set; } = true;
    public McpClientOptions McpClientOptions { get; set; }
    public List<McpServerConfigModel> McpServerConfigs { get; set; } = [];

    /// <summary>
    /// How long a server's tool listing is reused before it is fetched again. Zero disables the
    /// cache and lists the tools on every agent load, which is what this did before.
    /// </summary>
    /// <remarks>
    /// A listing costs a session of its own -- handshake, notification and stream -- on every
    /// agent load, for a list that changes when a server is redeployed rather than between two
    /// messages of one conversation. Sixty seconds is short enough that a tool added upstream
    /// shows up while someone is still testing it, and long enough that no conversation pays for
    /// the listing twice.
    /// </remarks>
    public int ToolListCacheSeconds { get; set; } = 60;
}
