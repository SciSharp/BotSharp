using BotSharp.Plugin.MessageQueue.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BotSharp.Plugin.MessageQueue.Consumers;

public abstract class MQConsumerBase : IDisposable
{
    protected readonly IServiceProvider _services;
    protected readonly IMQConnection _mqConnection;
    protected readonly ILogger _logger;

    private IChannel? _channel;
    private bool _disposed = false;

    protected abstract string ExchangeName { get; }
    protected abstract string QueueName { get; }
    protected abstract string RoutingKey { get; }

    protected MQConsumerBase(
        IServiceProvider services,
        IMQConnection mqConnection,
        ILogger logger)
    {
        _services = services;
        _mqConnection = mqConnection;
        _logger = logger;
        InitAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    protected abstract Task<bool> OnMessageReceiveHandle(string data);

    private async Task InitAsync()
    {
        _channel = await CreateChannelAsync();
        await InitConsumeAsync();
    }

    private async Task<IChannel> CreateChannelAsync()
    {
        if (!_mqConnection.IsConnected)
        {
            await _mqConnection.ConnectAsync();
        }

        var channel = await _mqConnection.CreateChannelAsync();
        _logger.LogWarning($"Created Rabbit MQ channel {channel.ChannelNumber}");

        var args = new Dictionary<string, object?>
        {
            ["x-delayed-type"] = "direct"
        };

        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: "x-delayed-message",
            durable: true,
            autoDelete: false,
            arguments: args);

        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        await channel.QueueBindAsync(queue: QueueName, exchange: ExchangeName, routingKey: RoutingKey);
        channel.ChannelShutdownAsync += async (sender, evt) =>
        {
            if (_disposed || !_mqConnection.IsConnected)
            {
                return;
            }

            _channel?.Dispose();
            await InitAsync();
        };

        return channel;
    }

    private async Task InitConsumeAsync()
    {
        _logger.LogWarning($"Rabbit MQ starts consuming ({QueueName}) message.");

        if (_channel == null)
        {
            throw new Exception($"Undefined channel for queue {QueueName}.");
        }
        
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += ConsumeEventAsync;
        await _channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: consumer);

        _logger.LogWarning($"Rabbit MQ consumed ({QueueName}) message.");
    }

    private async Task ConsumeEventAsync(object sender, BasicDeliverEventArgs eventArgs)
    {
        var data = string.Empty;
        try
        {
            data = Encoding.UTF8.GetString(eventArgs.Body.Span);
            _logger.LogInformation($"{GetType().Name} message id:{eventArgs.BasicProperties?.MessageId}, data: {data}");
            await OnMessageReceiveHandle(data);

            await _channel!.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when Rabbit MQ consumes data ({data}) in {QueueName}.");
            await _channel!.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing || _disposed)
        {
            return;
        }

        _logger.LogWarning($"Start disposing consumer channel: {QueueName}");
        if (_channel != null)
        {
            _channel.Dispose();
            _disposed = true;
            _logger.LogWarning($"Disposed consumer channel: {QueueName}");
        }
    }
}

