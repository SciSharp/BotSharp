namespace BotSharp.Plugin.Planner.SqlGeneration.Models;

public class PrimaryRequirementRequest
{
    [JsonPropertyName("requirement_detail")]
    public string Requirements { get; set; } = null!;

    [JsonPropertyName("questions")]
    public string[] Questions { get; set; } = [];

    [JsonPropertyName("norm_questions")]
    public string[] NormQuestions { get; set; } = [];
}
