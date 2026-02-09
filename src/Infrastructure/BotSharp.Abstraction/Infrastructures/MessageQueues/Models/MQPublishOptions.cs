using System.Text.Json;

namespace BotSharp.Abstraction.Infrastructures.MessageQueues.Models;

/// <summary>
/// Configuration options for publishing messages to a message queue.
/// These options are MQ-product agnostic and can be adapted by different implementations.
/// </summary>
public class MQPublishOptions
{
    /// <summary>
    /// The topic name (exchange in RabbitMQ, topic in Kafka/Azure Service Bus).
    /// </summary>
    public string TopicName { get; set; } = string.Empty;

    /// <summary>
    /// The routing key (partition key in some MQ systems, used for message routing).
    /// </summary>
    public string RoutingKey { get; set; } = string.Empty;

    /// <summary>
    /// Delay in milliseconds before the message is delivered.
    /// </summary>
    public long DelayMilliseconds { get; set; }

    /// <summary>
    /// Optional unique identifier for the message.
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Additional arguments for the publish configuration (MQ-specific).
    /// </summary>
    public Dictionary<string, object?> Arguments { get; set; } = [];

    /// <summary>
    /// Json serializer options
    /// </summary>
    public JsonSerializerOptions? JsonOptions { get; set; }
}
