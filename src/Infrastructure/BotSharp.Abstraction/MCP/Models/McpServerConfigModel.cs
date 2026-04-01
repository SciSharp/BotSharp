namespace BotSharp.Abstraction.MCP.Models;

public class McpServerConfigModel
{
    /// <summary>
    /// Unique identifier for this server configuration.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Display name for the server.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Indicates whether this server is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// URL of the icon to display for this server.
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// Short description of this server.
    /// </summary>
    public string? Description { get; set; }

    public McpSseServerConfig? SseConfig { get; set; }
    public McpStdioServerConfig? StdioConfig { get; set; }
}

public class McpSseServerConfig
{
    public string EndPoint { get; set; } = null!;
    public TimeSpan ConnectionTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public Dictionary<string, string>? AdditionalHeaders { get; set; }
}

public class McpStdioServerConfig
{
    public string Command { get; set; } = null!;
    public IList<string>? Arguments { get; set; }
    public Dictionary<string, string>? EnvironmentVariables { get; set; }
    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(5);
}