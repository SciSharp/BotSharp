namespace BotSharp.Plugin.RabbitMQ.Models;

internal class RabbitMQConsumerConfig
{
    /// <summary>
    /// The exchange name (topic in some MQ systems).
    /// </summary>
    internal string ExchangeName { get; set; } = "rabbitmq.exchange";

    /// <summary>
    /// The queue name (subscription in some MQ systems).
    /// </summary>
    internal string QueueName { get; set; } = "rabbitmq.queue";

    /// <summary>
    /// The routing key (filter in some MQ systems).
    /// </summary>
    internal string RoutingKey { get; set; } = "rabbitmq.routing";

    /// <summary>
    /// Additional arguments for the consumer configuration.
    /// </summary>
    internal Dictionary<string, object?> Arguments { get; set; } = new();
}
