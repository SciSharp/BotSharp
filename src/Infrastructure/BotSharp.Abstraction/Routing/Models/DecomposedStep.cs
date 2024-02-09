namespace BotSharp.Abstraction.Routing.Models;

public class DecomposedStep
{
    public string Description { get; set; }

    [JsonPropertyName("total_remaining_steps")]
    public int TotalRemainingSteps { get; set; }

    [JsonPropertyName("should_stop")]
    public bool ShouldStop { get; set; }

    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }
}
