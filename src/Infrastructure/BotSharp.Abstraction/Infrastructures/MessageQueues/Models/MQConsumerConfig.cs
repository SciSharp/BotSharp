namespace BotSharp.Abstraction.Infrastructures.MessageQueues.Models;

/// <summary>
/// Configuration options for message queue consumers.
/// These options are MQ-product agnostic and can be adapted by different implementations.
/// </summary>
public class MQConsumerConfig
{
    /// <summary>
    /// The exchange name (topic in some MQ systems).
    /// </summary>
    public string ExchangeName { get; set; } = string.Empty;

    /// <summary>
    /// The queue name (subscription in some MQ systems).
    /// </summary>
    public string QueueName { get; set; } = string.Empty;

    /// <summary>
    /// The routing key (filter in some MQ systems).
    /// </summary>
    public string RoutingKey { get; set; } = string.Empty;

    /// <summary>
    /// Whether to automatically acknowledge messages.
    /// </summary>
    public bool AutoAck { get; set; } = false;

    /// <summary>
    /// Additional arguments for the consumer configuration.
    /// </summary>
    public Dictionary<string, object?> Arguments { get; set; } = new();
}

