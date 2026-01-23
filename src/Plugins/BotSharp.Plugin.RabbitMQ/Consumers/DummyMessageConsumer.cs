namespace BotSharp.Plugin.RabbitMQ.Consumers;

public class DummyMessageConsumer : MQConsumerBase
{
    public override MQConsumerConfig Config => new()
    {
        ExchangeName = "my.exchange",
        QueueName = "dummy.queue",
        RoutingKey = "my.routing"
    };

    public DummyMessageConsumer(
        IServiceProvider services,
        ILogger<DummyMessageConsumer> logger)
        : base(services, logger)
    {
    }

    public override async Task<bool> HandleMessageAsync(string channel, string data)
    {
        _logger.LogCritical($"Received delayed dummy message data: {data}");
        return await Task.FromResult(true);
    }
}
