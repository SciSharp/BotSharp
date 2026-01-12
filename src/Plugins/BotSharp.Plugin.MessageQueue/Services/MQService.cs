using BotSharp.Plugin.MessageQueue.Interfaces;
using RabbitMQ.Client;
using System.Collections.Concurrent;

namespace BotSharp.Plugin.MessageQueue.Services;

public class MQService : IMQService
{
    private IMQConnection _mqConnection;
    private readonly ILogger<MQService> _logger;

    private static readonly ConcurrentDictionary<string, MQConsumerBase> _consumers = [];

    public MQService(
        IMQConnection mqConnection,
        ILogger<MQService> logger)
    {
        _mqConnection = mqConnection;
        _logger = logger;
    }

    public void Subscribe(string key, object consumer)
    {
        var baseConsumer = consumer as MQConsumerBase;
        if (baseConsumer != null)
        {
            _consumers.TryAdd(key, baseConsumer);
        }
    }

    public async Task<bool> PublishAsync<T>(T payload, string exchange, string routingkey, long milliseconds = 0, string messageId = "")
    {
        if (!_mqConnection.IsConnected)
        {
            await _mqConnection.ConnectAsync();
        }

        await using var channel = await _mqConnection.CreateChannelAsync();
        var args = new Dictionary<string, object?>
        {
            ["x-delayed-type"] = "direct"
        };

        await channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: "x-delayed-message",
            durable: true,
            autoDelete: false,
            arguments: args);

        var message = new MQMessage<T>(payload, messageId);
        var body = ConvertToBinary(message);
        var properties = new BasicProperties
        {
            MessageId = messageId,
            DeliveryMode = DeliveryModes.Persistent,
            Headers = new Dictionary<string, object?>
            {
                ["x-delay"] = milliseconds
            }
        };

        await channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingkey,
            mandatory: true,
            basicProperties: properties,
            body: body);
        return true;
    }

    private byte[] ConvertToBinary<T>(T data)
    {
        var jsonStr = JsonSerializer.Serialize(data);
        var body = Encoding.UTF8.GetBytes(jsonStr);
        return body;
    }
}
