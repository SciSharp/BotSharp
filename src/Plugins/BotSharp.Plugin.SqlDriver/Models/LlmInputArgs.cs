using System.Text.Json.Serialization;

namespace BotSharp.Plugin.SqlHero.Models;

public class LlmInputArgs
{
    [JsonPropertyName("sql_statement")]
    public string SqlStatement { get; set; }
}
