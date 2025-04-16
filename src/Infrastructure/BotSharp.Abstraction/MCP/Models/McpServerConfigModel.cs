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