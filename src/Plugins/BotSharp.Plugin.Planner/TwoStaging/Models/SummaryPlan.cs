namespace BotSharp.Plugin.Planner.TwoStaging.Models;

public class SummaryPlan
{
    [JsonPropertyName("is_sql_template")]
    public bool IsSqlTemplate { get; set; } = false;
}
