namespace BotSharp.Plugin.Membase.Models;

public class CypherQueryRequest
{
    public string Query { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = [];
    public bool IncludeExecutionPlan { get; set; } = false;

    /// <summary>
    /// Whether to profile the query execution.
    /// </summary>
    public bool Profile { get; set; } = false;
    public int? TimeoutMs { get; set; }
}
