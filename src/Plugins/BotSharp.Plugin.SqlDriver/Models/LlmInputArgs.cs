using System.Text.Json.Serialization;

namespace BotSharp.Plugin.SqlDriver.Models;

public class LlmInputArgs
{
    [JsonPropertyName("sql_statement")]
    public string SqlStatement { get; set; }
}
