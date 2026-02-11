namespace BotSharp.Plugin.RabbitMQ.Models;

/// <summary>
/// Payload for scheduled/delayed messages
/// </summary>
public class ScheduledMessagePayload
{
    public string Name { get; set; }
}
