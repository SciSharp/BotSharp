using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;

namespace BotSharp.Plugin.RabbitMQ.Services;

public class RabbitMQService : IMQService
{
    private readonly IRabbitMQConnection _mqConnection;
    private readonly IServiceProvider _services;
    private readonly ILogger<RabbitMQService> _logger;

    private readonly int _retryCount = 5;
    private bool _disposed = false;
    private static readonly ConcurrentDictionary<string, ConsumerRegistration> _consumers = [];

    public RabbitMQService(
        IRabbitMQConnection mqConnection,
        IServiceProvider services,
        ILogger<RabbitMQService> logger)
    {
        _mqConnection = mqConnection;
        _services = services;
        _logger = logger;
    }

    public async Task<bool> SubscribeAsync(string key, IMQConsumer consumer)
    {
        if (_consumers.ContainsKey(key))
        {
            _logger.LogWarning($"Consumer with key '{key}' is already subscribed.");
            return false;
        }

        var registration = await CreateConsumerRegistrationAsync(consumer);
        if (registration != null && _consumers.TryAdd(key, registration))
        {
            var config = consumer.Config as RabbitMQConsumerConfig ?? new();
            _logger.LogInformation($"Consumer '{key}' subscribed to queue '{config.QueueName}'.");
            return true;
        }

        return false;
    }

    public async Task<bool> UnsubscribeAsync(string key)
    {
        if (!_consumers.TryRemove(key, out var registration))
        {
            return false;
        }

        try
        {
            if (registration.Channel != null)
            {
                registration.Channel.Dispose();
            }
            registration.Consumer.Dispose();
            _logger.LogInformation($"Consumer '{key}' unsubscribed.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error unsubscribing consumer '{key}'.");
            return false;
        }
    }

    private async Task<ConsumerRegistration?> CreateConsumerRegistrationAsync(IMQConsumer consumer)
    {
        try
        {
            var channel = await CreateChannelAsync(consumer);

            var config = consumer.Config as RabbitMQConsumerConfig ?? new();
            var registration = new ConsumerRegistration(consumer, channel);

            var asyncConsumer = new AsyncEventingBasicConsumer(channel);
            asyncConsumer.ReceivedAsync += async (sender, eventArgs) =>
            {
                await ConsumeEventAsync(registration, eventArgs);
            };

            await channel.BasicConsumeAsync(
                queue: config.QueueName,
                autoAck: config.AutoAck,
                consumer: asyncConsumer);

            _logger.LogWarning($"RabbitMQ consuming queue '{config.QueueName}'.");
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

        var config = consumer.Config as RabbitMQConsumerConfig ?? new();
        var channel = await _mqConnection.CreateChannelAsync();
        _logger.LogWarning($"Created RabbitMQ channel {channel.ChannelNumber} for queue '{config.QueueName}'");

        var args = new Dictionary<string, object?>
        {
            ["x-delayed-type"] = "direct"
        };

        if (config.Arguments != null)
        {
            foreach (var kvp in config.Arguments)
            {
                args[kvp.Key] = kvp.Value;
            }
        }

        await channel.ExchangeDeclareAsync(
            exchange: config.ExchangeName,
            type: "x-delayed-message",
            durable: true,
            autoDelete: false,
            arguments: args);

        await channel.QueueDeclareAsync(
            queue: config.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        await channel.QueueBindAsync(
            queue: config.QueueName,
            exchange: config.ExchangeName,
            routingKey: config.RoutingKey);

        return channel;
    }

    private async Task ConsumeEventAsync(ConsumerRegistration registration, BasicDeliverEventArgs eventArgs)
    {
        var data = string.Empty;
        var config = registration.Consumer.Config as RabbitMQConsumerConfig ?? new();

        try
        {
            data = Encoding.UTF8.GetString(eventArgs.Body.Span);
            _logger.LogInformation($"Message received on '{config.QueueName}', id: {eventArgs.BasicProperties?.MessageId}, data: {data}");

            var isHandled = await registration.Consumer.HandleMessageAsync(config.QueueName, data);
            if (!config.AutoAck && registration.Channel != null)
            {
                if (isHandled)
                {
                    await registration.Channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
                }
                else
                {
                    await registration.Channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error consuming message on queue '{config.QueueName}': {data}");
            if (!config.AutoAck && registration.Channel != null)
            {
                await registration.Channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
        }
    }

    public async Task<bool> PublishAsync<T>(T payload, MQPublishOptions options)
    {
        try
        {
            if (options == null)
            {
                return false;
            }

            if (!_mqConnection.IsConnected)
            {
                await _mqConnection.ConnectAsync();
            }

            var isPublished = false;
            var policy = BuildRetryPolicy();
            await policy.Execute(async () =>
            {
                var channelPool = RabbitMQChannelPoolFactory.GetChannelPool(_services, _mqConnection);
                var channel = channelPool.Get();

                try
                {
                    var args = new Dictionary<string, object?>
                    {
                        ["x-delayed-type"] = "direct"
                    };

                    if (!options.Arguments.IsNullOrEmpty())
                    {
                        foreach (var kvp in options.Arguments)
                        {
                            args[kvp.Key] = kvp.Value;
                        }
                    }

                    await channel.ExchangeDeclareAsync(
                        exchange: options.TopicName,
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
                            ["x-delay"] = options.DelayMilliseconds
                        }
                    };

                    await channel.BasicPublishAsync(
                        exchange: options.TopicName,
                        routingKey: options.RoutingKey,
                        mandatory: true,
                        basicProperties: properties,
                        body: body);

                    isPublished = true;
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    channelPool.Return(channel);
                }
            });

            return isPublished;
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
            _retryCount,
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

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogWarning($"Disposing {nameof(RabbitMQService)}");

        foreach (var item in _consumers)
        {
            if (item.Value.Channel != null)
            {
                item.Value.Channel.Dispose();
            }
            item.Value.Consumer.Dispose();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
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
