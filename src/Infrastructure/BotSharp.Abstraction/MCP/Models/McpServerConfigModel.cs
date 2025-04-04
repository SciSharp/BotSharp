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
    /// The type of transport to use.
    /// </summary>
    [JsonPropertyName("transport_type")]
    public string TransportType { get; set; } = null!;

    /// <summary>
    /// For stdio transport: path to the executable
    /// For HTTP transport: base URL of the server
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Additional transport-specific configuration.
    /// </summary>
    [JsonPropertyName("transport_options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? TransportOptions { get; set; }
}
