using System.Text.Json.Serialization;

namespace BotSharp.Abstraction.Knowledges.Models;

public class RetrievedResult
{
    public int Paragraph { get; set; }

    [JsonPropertyName("cite_source")]
    public string CiteSource { get; set; } = "related text";

    [JsonPropertyName("reasoning")]
    public string Reasoning { get; set; } = "";
}
