namespace BotSharp.Plugin.RabbitMQ.Models;

/// <summary>
/// Request model for publishing a scheduled message
/// </summary>
public class PublishScheduledMessageRequest
{
    public string? Name { get; set; }

    public long? DelayMilliseconds { get; set; }

    public string? MessageId { get; set; }
}


/// <summary>
/// Response model for publish operations
/// </summary>
public class PublishMessageResponse
{
    /// <summary>
    /// Whether the message was successfully published
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if publish failed
    /// </summary>
    public string? Error { get; set; }
}

