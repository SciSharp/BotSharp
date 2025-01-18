namespace BotSharp.Plugin.Planner.SqlGeneration.Models;

public class SqlSecondStagePlan
{
    [JsonPropertyName("related_tables")]
    public string[] Tables { get; set; } = [];

    [JsonPropertyName("need_lookup_dictionary")]
    public bool NeedLookupDictionary { get; set; } = false;

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
}
