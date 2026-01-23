namespace BotSharp.Plugin.RabbitMQ.Consumers;

public class ScheduledMessageConsumer : MQConsumerBase
{
    public override object Config => new
    {
        ExchangeName = "my.exchange",
        QueueName = "scheduled.queue",
        RoutingKey = "my.routing"
    };

    public ScheduledMessageConsumer(
        IServiceProvider services,
        ILogger<ScheduledMessageConsumer> logger)
        : base(services, logger)
    {
    }

    public override async Task<bool> HandleMessageAsync(string channel, string data)
    {
        _logger.LogCritical($"Received delayed scheduled message data: {data}");
        return await Task.FromResult(true);
    }
}

