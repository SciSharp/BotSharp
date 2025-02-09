namespace BotSharp.Plugin.Planner.SqlGeneration.Models;

public class SqlPrimaryStagePlan
{
    [JsonPropertyName("task_detail")]
    public string Task { get; set; } = "";

    [JsonPropertyName("step")]
    public int Step { get; set; } = -1;

    [JsonPropertyName("need_breakdown_task")]
    public bool NeedAdditionalInformation { get; set; } = false;

    [JsonPropertyName("need_lookup_dictionary")]
    public bool NeedLookupDictionary { get; set; } = false;

    [JsonPropertyName("related_tables")]
    public string[] Tables { get; set; } = [];

    [JsonPropertyName("has_found_relevant_knowledge")]
    public bool HasFoundRelevantKnowledge { get; set; } = false;

    public override string ToString()
    {
        return $"STEP {Step}: {Task}";
    }
}
