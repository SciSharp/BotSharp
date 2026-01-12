using BotSharp.Plugin.MessageQueue.Interfaces;

namespace BotSharp.Plugin.MessageQueue.Consumers;


public class ScheduledMessageConsumer : MQConsumerBase
{
    protected override string ExchangeName => "scheduled.exchange";
    protected override string QueueName => "scheduled.queue";
    protected override string RoutingKey => "scheduled.routing";

    public ScheduledMessageConsumer(
        IServiceProvider services,
        IMQConnection mqConnection,
        ILogger<ScheduledMessageConsumer> logger)
        : base(services, mqConnection, logger)
    {
    }

    protected override async Task<bool> OnMessageReceiveHandle(string data)
    {
        _logger.LogCritical($"Received delayed message data: {data}");
        return true;
    }
}

