using System.Text.Json.Serialization;

namespace BotSharp.Plugin.SqlDriver.Models;

public class LookupDictionary
{
    [JsonPropertyName("sql_statement")]
    public string SqlStatement { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("table")]
    public string Table { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"Sql: {SqlStatement}, Table: {Table}, Reason: {Reason}";
    }
}
