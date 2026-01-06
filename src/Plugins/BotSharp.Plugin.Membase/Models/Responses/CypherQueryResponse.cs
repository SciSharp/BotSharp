namespace BotSharp.Plugin.Membase.Models;

public class CypherQueryResponse
{
    public string[] Columns { get; set; } = [];
    public Dictionary<string, object?>[] Data { get; set; } = [];

    public CypherNotification[] Notifications { get; set; } = [];
    public int RowCount { get; set; }
}

public class CypherNotification
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
