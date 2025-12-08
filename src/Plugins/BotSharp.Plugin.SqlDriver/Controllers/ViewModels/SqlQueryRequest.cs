namespace BotSharp.Plugin.SqlDriver.Controllers.ViewModels;

public class SqlQueryRequest
{
    public string DbType { get; set; } = null!;
    public string SqlStatement { get; set; } = null!;
    public bool FormattingResult { get; set; } = true;
}
