namespace BotSharp.Plugin.Planner.SqlGeneration.Models;

public class SecondStagePlan
{
    [JsonPropertyName("related_tables")]
    public string[] Tables { get; set; } = [];

    [JsonPropertyName("need_lookup_dictionary")]
    public bool NeedLookupDictionary { get; set; } = false;

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("input_args")]
    public JsonDocument[] Parameters { get; set; } = [];

    [JsonPropertyName("output_results")]
    public string[] Results { get; set; } = [];
}
