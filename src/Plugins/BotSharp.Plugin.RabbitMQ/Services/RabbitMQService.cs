using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;

namespace BotSharp.Plugin.RabbitMQ.Services;

public class RabbitMQService : IMQService
{
    private readonly IRabbitMQConnection _mqConnection;
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQService> _logger;

    private static readonly ConcurrentDictionary<string, ConsumerRegistration> _consumers = [];

    public RabbitMQService(
        IRabbitMQConnection mqConnection,
        RabbitMQSettings settings,
        ILogger<RabbitMQService> logger)
    {
        _mqConnection = mqConnection;
        _settings = settings;
        _logger = logger;
    }

    public async Task SubscribeAsync(string key, IMQConsumer consumer)
    {
        if (_consumers.ContainsKey(key))
        {
            _logger.LogWarning($"Consumer with key '{key}' is already subscribed.");
            return;
        }

        var registration = await CreateConsumerRegistrationAsync(consumer);
        if (registration != null && _consumers.TryAdd(key, registration))
        {
            _logger.LogInformation($"Consumer '{key}' subscribed to queue '{consumer.Options.QueueName}'.");
        }
    }

    public async Task UnsubscribeAsync(string key)
    {
        if (_consumers.TryRemove(key, out var registration))
        {
            try
            {
                if (registration.Channel != null)
                {
                    registration.Channel.Dispose();
                }
                registration.Consumer.Dispose();
                _logger.LogInformation($"Consumer '{key}' unsubscribed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error unsubscribing consumer '{key}'.");
            }
        }
    }

    private async Task<ConsumerRegistration?> CreateConsumerRegistrationAsync(IMQConsumer consumer)
    {
        try
        {
            var channel = await CreateChannelAsync(consumer);

            var options = consumer.Options;
            var registration = new ConsumerRegistration(consumer, channel);

            var asyncConsumer = new AsyncEventingBasicConsumer(channel);
            asyncConsumer.ReceivedAsync += async (sender, eventArgs) =>
            {
                await ConsumeEventAsync(registration, eventArgs);
            };

            await channel.BasicConsumeAsync(
                queue: options.QueueName,
                autoAck: options.AutoAck,
                consumer: asyncConsumer);

            _logger.LogWarning($"RabbitMQ consuming queue '{options.QueueName}'.");
            return registration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when register consumer in RabbitMQ.");
            return null;
        }
    }

    private async Task<IChannel> CreateChannelAsync(IMQConsumer consumer)
    {
        if (!_mqConnection.IsConnected)
        {
            await _mqConnection.ConnectAsync();
        }

        var options = consumer.Options;
        var channel = await _mqConnection.CreateChannelAsync();
        _logger.LogWarning($"Created RabbitMQ channel {channel.ChannelNumber} for queue '{options.QueueName}'");

        var args = new Dictionary<string, object?>
        {
            ["x-delayed-type"] = "direct"
        };

        if (options.Arguments != null)
        {
            foreach (var kvp in options.Arguments)
            {
                args[kvp.Key] = kvp.Value;
            }
        }

        await channel.ExchangeDeclareAsync(
            exchange: options.ExchangeName,
            type: "x-delayed-message",
            durable: true,
            autoDelete: false,
            arguments: args);

        await channel.QueueDeclareAsync(
            queue: options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        await channel.QueueBindAsync(
            queue: options.QueueName,
            exchange: options.ExchangeName,
            routingKey: options.RoutingKey);

        return channel;
    }

    private async Task ConsumeEventAsync(ConsumerRegistration registration, BasicDeliverEventArgs eventArgs)
    {
        var data = string.Empty;
        var options = registration.Consumer.Options;

        try
        {
            data = Encoding.UTF8.GetString(eventArgs.Body.Span);
            _logger.LogInformation($"Message received on '{options.QueueName}', id: {eventArgs.BasicProperties?.MessageId}, data: {data}");

            await registration.Consumer.HandleMessageAsync(options.QueueName, data);

            if (!options.AutoAck && registration.Channel != null)
            {
                await registration.Channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error consuming message on queue '{options.QueueName}': {data}");
            if (!options.AutoAck && registration.Channel != null)
            {
                await registration.Channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
        }
    }

    public async Task<bool> PublishAsync<T>(T payload, MQPublishOptions options)
    {
        try
        {
            if (!_mqConnection.IsConnected)
            {
                await _mqConnection.ConnectAsync();
            }

            var policy = BuildRetryPolicy();
            await policy.Execute(async () =>
            {
                await using var channel = await _mqConnection.CreateChannelAsync();
                var args = new Dictionary<string, object?>
                {
                    ["x-delayed-type"] = "direct"
                };

                if (options.Arguments != null)
                {
                    foreach (var kvp in options.Arguments)
                    {
                        args[kvp.Key] = kvp.Value;
                    }
                }

                await channel.ExchangeDeclareAsync(
                    exchange: options.Exchange,
                    type: "x-delayed-message",
                    durable: true,
                    autoDelete: false,
                    arguments: args);

                var messageId = options.MessageId ?? Guid.NewGuid().ToString();
                var message = new MQMessage<T>(payload, messageId);
                var body = ConvertToBinary(message);
                var properties = new BasicProperties
                {
                    MessageId = messageId,
                    DeliveryMode = DeliveryModes.Persistent,
                    Headers = new Dictionary<string, object?>
                    {
                        ["x-delay"] = options.MilliSeconds
                    }
                };

                await channel.BasicPublishAsync(
                    exchange: options.Exchange,
                    routingKey: options.RoutingKey,
                    mandatory: true,
                    basicProperties: properties,
                    body: body);
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when RabbitMQ publish message.");
            return false;
        }
    }

    private RetryPolicy BuildRetryPolicy()
    {
        return Policy.Handle<Exception>().WaitAndRetry(
            _settings.RetryCount,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            (ex, time) =>
            {
                _logger.LogError(ex, $"RabbitMQ publish error: after {time.TotalSeconds:n1}s");
            });
    }

    private byte[] ConvertToBinary<T>(T data)
    {
        var jsonStr = JsonSerializer.Serialize(data);
        var body = Encoding.UTF8.GetBytes(jsonStr);
        return body;
    }

    /// <summary>
    /// Internal class to track consumer registrations with their RabbitMQ channels.
    /// </summary>
    private class ConsumerRegistration
    {
        public IMQConsumer Consumer { get; }
        public IChannel? Channel { get; }

        public ConsumerRegistration(IMQConsumer consumer, IChannel? channel)
        {
            Consumer = consumer;
            Channel = channel;
        }
    }
}
