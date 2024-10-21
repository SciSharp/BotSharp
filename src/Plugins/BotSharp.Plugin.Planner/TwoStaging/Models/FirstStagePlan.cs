namespace BotSharp.Plugin.Planner.TwoStaging.Models;

public class FirstStagePlan
{
    [JsonPropertyName("task_detail")]
    public string Task { get; set; } = "";

    //[JsonPropertyName("reason")]
    //public string Reason { get; set; } = "";

    [JsonPropertyName("step")]
    public int Step { get; set; } = -1;

    [JsonPropertyName("need_breakdown_task")]
    public bool NeedAdditionalInformation { get; set; } = false;

    [JsonPropertyName("need_lookup_dictionary")]
    public bool NeedLookupDictionary { get; set; } = false;

    [JsonPropertyName("related_tables")]
    public string[] Tables { get; set; } = [];

    //[JsonPropertyName("related_urls")]
    //public string[] Urls { get; set; } = [];

    //[JsonPropertyName("input_args")]
    //public JsonDocument[] Parameters { get; set; } = [];

    //[JsonPropertyName("output_results")]
    //public string[] Results { get; set; } = [];

    public override string ToString()
    {
        return $"STEP {Step}: {Task}";
    }
}
