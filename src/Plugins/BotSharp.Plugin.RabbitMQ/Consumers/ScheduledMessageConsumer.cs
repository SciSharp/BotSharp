using BotSharp.Abstraction.Infrastructures.MessageQueues.Models;

namespace BotSharp.Plugin.RabbitMQ.Consumers;

public class ScheduledMessageConsumer : MQConsumerBase
{
    public override MQConsumerOptions Options => new()
    {
        ExchangeName = "scheduled.exchange",
        QueueName = "scheduled.queue",
        RoutingKey = "scheduled.routing"
    };

    public ScheduledMessageConsumer(
        IServiceProvider services,
        ILogger<ScheduledMessageConsumer> logger)
        : base(services, logger)
    {
    }

    public override async Task<bool> HandleMessageAsync(string channel, string data)
    {
        _logger.LogCritical($"Received delayed message data: {data}");
        return await Task.FromResult(true);
    }
}

