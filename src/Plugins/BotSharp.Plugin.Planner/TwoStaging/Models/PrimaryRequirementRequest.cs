namespace BotSharp.Plugin.Planner.TwoStaging.Models;

public class PrimaryRequirementRequest
{
    [JsonPropertyName("requirement_detail")]
    public string Requirements { get; set; } = null!;

    [JsonPropertyName("questions")]
    public string[] Questions { get; set; } = [];
}
