namespace BotSharp.Plugin.Planner.TwoStaging.Models;

public class PrimaryRequirementRequest
{
    [JsonPropertyName("requirement_detail")]
    public string Requirements { get; set; } = null!;

    [JsonPropertyName("has_knowledge_reference")]
    public bool HasKnowledgeReference { get; set; }

    [JsonPropertyName("question")]
    public string Question { get; set; } = null!;
}
