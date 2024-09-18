using System.Text.Json.Serialization;

namespace BotSharp.Plugin.SqlDriver.Models;

public class ExecuteQueryArgs
{
    [JsonPropertyName("sql_statements")]
    public string[] SqlStatements { get; set; } = [];
}
