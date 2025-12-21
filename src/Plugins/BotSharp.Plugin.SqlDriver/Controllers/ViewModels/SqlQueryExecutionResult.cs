namespace BotSharp.Plugin.SqlDriver.Controllers.ViewModels;

public class SqlQueryExecutionResult
{
    public string AgentId { get; set; } = null!;
    public string SqlUniqueId { get; set; } = null!;
    public string DbType { get; set; } = null!;
    public string Results { get; set; } = null!;
    public string ResultFormat { get; set; } = "markdown";
}
