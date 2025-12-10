using System.Text.Json.Serialization;

namespace BotSharp.Plugin.SqlDriver.Models;

public class ExecuteQueryArgs
{
    [JsonPropertyName("sql_statements")]
    public string[] SqlStatements { get; set; } = [];

    [JsonPropertyName("tables")]
    public string[] Tables { get; set; } = [];

    /// <summary>
    /// Beautifying query result
    /// </summary>
    [JsonPropertyName("result_format")]
    public string ResultFormat { get; set; } = "markdown";
}
