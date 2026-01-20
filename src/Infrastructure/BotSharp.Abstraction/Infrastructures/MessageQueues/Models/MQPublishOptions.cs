namespace BotSharp.Abstraction.Infrastructures.MessageQueues.Models;

public class MQPublishOptions
{
    public string Exchange { get; set; }
    public string RoutingKey { get; set; }
    public long MilliSeconds { get; set; }
    public string? MessageId { get; set; }
}
