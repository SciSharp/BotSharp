namespace BotSharp.Plugin.Planner.SqlGeneration.Models;

public class SqlReviewArgs
{
    [JsonPropertyName("is_sql_template")]
    public bool IsSqlTemplate { get; set; } = false;

    [JsonPropertyName("contains_sql_statements")]
    public bool ContainsSqlStatements { get; set; } = false;

    [JsonPropertyName("sql_statement")]
    public string SqlStatement { get; set; } = string.Empty;
}
