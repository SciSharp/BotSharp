using BotSharp.Abstraction.Infrastructures.MessageQueues.Models;

namespace BotSharp.Abstraction.Infrastructures.MessageQueues;

/// <summary>
/// Abstract interface for message queue consumers.
/// Implement this interface to create consumers that are independent of MQ products (e.g., RabbitMQ, Kafka, Azure Service Bus).
/// </summary>
public interface IMQConsumer : IDisposable
{
    /// <summary>
    /// Gets the consumer config
    /// </summary>
    object Config { get; }

    /// <summary>
    /// Handles the received message from the queue.
    /// </summary>
    /// <param name="channel">The consumer channel identifier</param>
    /// <param name="data">The message data as string</param>
    /// <returns>True if the message was handled successfully, false otherwise</returns>
    Task<bool> HandleMessageAsync(string channel, string data);
}

