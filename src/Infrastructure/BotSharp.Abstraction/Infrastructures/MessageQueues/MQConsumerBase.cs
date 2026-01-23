using BotSharp.Abstraction.Infrastructures.MessageQueues;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.RabbitMQ.Consumers;

/// <summary>
/// Abstract base class for RabbitMQ consumers.
/// Implements IMQConsumer to allow other projects to define consumers independently of RabbitMQ.
/// The RabbitMQ-specific infrastructure is handled by RabbitMQService.
/// </summary>
public abstract class MQConsumerBase : IMQConsumer
{
    protected readonly IServiceProvider _services;
    protected readonly ILogger _logger;
    private bool _disposed = false;

    /// <summary>
    /// Gets the consumer config for this consumer.
    /// Override this property to customize exchange, queue and routing configuration.
    /// </summary>
    public abstract object Config { get; }

    protected MQConsumerBase(
        IServiceProvider services,
        ILogger logger)
    {
        _services = services;
        _logger = logger;
    }

    /// <summary>
    /// Handles the received message from the queue.
    /// </summary>
    /// <param name="channel">The consumer channel identifier</param>
    /// <param name="data">The message data as string</param>
    /// <returns>True if the message was handled successfully, false otherwise</returns>
    public abstract Task<bool> HandleMessageAsync(string channel, string data);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        var consumerName = GetType().Name;
        _logger.LogWarning($"Disposing consumer: {consumerName}");
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

