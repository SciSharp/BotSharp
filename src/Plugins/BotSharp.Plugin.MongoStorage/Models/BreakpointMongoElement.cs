namespace BotSharp.Plugin.MongoStorage.Models;

public class BreakpointMongoElement
{
    public string? MessageId { get; set; }
    public DateTime Breakpoint { get; set; }
    public DateTime CreatedTime { get; set; }
}
