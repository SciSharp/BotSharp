using System.Text.Json.Serialization;

namespace BotSharp.Core.Routing.Planning;

public class FirstStagePlan
{
    [JsonPropertyName("task_detail")]
    public string Task { get; set; } = "";

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = "";

    [JsonPropertyName("step")]
    public int Step { get; set; } = -1;

    [JsonPropertyName("contain_multiple_steps")]
    public bool ContainMultipleSteps { get; set; } = false;

    [JsonPropertyName("related_tables")]
    public string[] Tables { get; set; } = new string[0];

    [JsonPropertyName("related_urls")]
    public string[] Urls { get; set; } = new string[0];

    [JsonPropertyName("input_args")]
    public JsonDocument[] Parameters { get; set; } = new JsonDocument[0];

    [JsonPropertyName("output_results")]
    public string[] Results { get; set; } = new string[0];

    public override string ToString()
    {
        return $"STEP {Step}: {Task}";
    }
}
