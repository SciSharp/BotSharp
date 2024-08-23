namespace BotSharp.Plugin.Planner.TwoStaging.Models;

public class SecondStagePlan
{
    [JsonPropertyName("related_tables")]
    public string[] Tables { get; set; } = new string[0];

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("tool_name")]
    public string Tool { get; set; } = "";

    [JsonPropertyName("input_args")]
    public JsonDocument[] Parameters { get; set; } = new JsonDocument[0];

    [JsonPropertyName("output_results")]
    public string[] Results { get; set; } = new string[0];
}
