namespace BotSharp.Abstraction.Knowledges.Models;

public class ExtractedKnowledge
{
    [JsonPropertyName("question")]
    public string Question { get; set; } = string.Empty;

    [JsonPropertyName("answer")]
    public string Answer { get; set; } = string.Empty;
}
