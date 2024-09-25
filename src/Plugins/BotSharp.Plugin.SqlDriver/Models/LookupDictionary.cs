using System.Text.Json.Serialization;

namespace BotSharp.Plugin.SqlDriver.Models;

public class LookupDictionary
{
    [JsonPropertyName("sql_statement")]
    public string SqlStatement { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; }

    [JsonPropertyName("table")]
    public string Table { get; set; }
}
