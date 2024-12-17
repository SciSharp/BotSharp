namespace BotSharp.Plugin.Planner.SqlGeneration.Models;

public class SecondaryBreakdownTask
{
    [JsonPropertyName("task_description")]
    public string TaskDescription { get; set; } = null!;

    [JsonPropertyName("solution_search_question")]
    public string SolutionQuestion { get; set; } = null!;

    [JsonPropertyName("need_lookup_dictionary")]
    public bool NeedLookupDictionary { get; set; }
}
