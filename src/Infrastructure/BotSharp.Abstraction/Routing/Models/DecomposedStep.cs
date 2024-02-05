namespace BotSharp.Abstraction.Routing.Models;

public class DecomposedStep
{
    public string Description { get; set; }

    [JsonPropertyName("total_remaining_steps")]
    public int TotalRemainingSteps { get; set; }

    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }
}
