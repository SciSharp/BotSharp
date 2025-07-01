using System.Text.Json.Serialization;

namespace BotSharp.Plugin.ChartHandler.LlmContext;

public class LlmContextIn
{
    [JsonPropertyName("plotting_requirement")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PlottingRequirement { get; set; }
}
