namespace BotSharp.Abstraction.Knowledges.Models;

public class GenerateKnowledge
{
    [JsonPropertyName("question")]
    public string Question { get; set; } = string.Empty;

    [JsonPropertyName("answer")]
    public string Answer { get; set; } = string.Empty;

    [JsonPropertyName("refined_collection")]
    public string RefinedCollection { get; set; } = string.Empty;

    [JsonPropertyName("refine_answer")]
    public Boolean RefineAnswer { get; set; } = false;

    [JsonPropertyName("existing_answer")]
    public string ExistingAnswer { get; set; } = string.Empty;
}
