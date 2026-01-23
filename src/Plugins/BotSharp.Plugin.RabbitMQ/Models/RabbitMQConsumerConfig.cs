namespace BotSharp.Plugin.RabbitMQ.Models;

internal class RabbitMQConsumerConfig
{
    /// <summary>
    /// The exchange name (topic in some MQ systems).
    /// </summary>
    public string ExchangeName { get; set; } = "rabbitmq.exchange";

    /// <summary>
    /// The queue name (subscription in some MQ systems).
    /// </summary>
    public string QueueName { get; set; } = "rabbitmq.queue";

    /// <summary>
    /// The routing key (filter in some MQ systems).
    /// </summary>
    public string RoutingKey { get; set; } = "rabbitmq.routing";

    /// <summary>
    /// Whether to automatically acknowledge messages.
    /// </summary>
    public bool AutoAck { get; set; } = false;

    /// <summary>
    /// Additional arguments for the consumer configuration.
    /// </summary>
    public Dictionary<string, object?> Arguments { get; set; } = new();
}
