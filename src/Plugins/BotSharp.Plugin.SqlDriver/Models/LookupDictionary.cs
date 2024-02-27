using System.Text.Json.Serialization;

namespace BotSharp.Plugin.SqlDriver.Models;

public class LookupDictionary
{
    [JsonPropertyName("table")]
    public string Table { get; set; }

    [JsonPropertyName("keyword")]
    public string Keyword { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; }

    [JsonPropertyName("columns")]
    public string[] Columns { get; set; }
}
