namespace BotSharp.Plugin.Membase.Models;

public class EdgeUpdateModel
{
    public string? Id { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}
