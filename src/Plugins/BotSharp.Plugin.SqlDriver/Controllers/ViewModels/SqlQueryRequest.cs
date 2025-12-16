namespace BotSharp.Plugin.SqlDriver.Controllers.ViewModels;

public class SqlQueryRequest
{
    public string AgentId { get; set; } = null!;
    public string DbType { get; set; } = null!;
    public string SqlStatement { get; set; } = null!;
    public string ResultFormat { get; set; } = "markdown";
    public bool IsEphemeral { get; set; } = false;
}
