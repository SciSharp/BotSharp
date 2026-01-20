using BotSharp.Abstraction.Infrastructures.MessageQueues.Models;

namespace BotSharp.Abstraction.Infrastructures.MessageQueues;

public interface IMQService
{
    /// <summary>
    /// Subscribe a consumer to the message queue.
    /// The consumer will be initialized with the appropriate MQ-specific infrastructure.
    /// </summary>
    /// <param name="key">Unique identifier for the consumer</param>
    /// <param name="consumer">The consumer implementing IMQConsumer interface</param>
    /// <returns>Task representing the async subscription operation</returns>
    Task SubscribeAsync(string key, IMQConsumer consumer);

    /// <summary>
    /// Unsubscribe a consumer from the message queue.
    /// </summary>
    /// <param name="key">Unique identifier for the consumer</param>
    /// <returns>Task representing the async unsubscription operation</returns>
    Task UnsubscribeAsync(string key);

    /// <summary>
    /// Publish payload to message queue
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="payload"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    Task<bool> PublishAsync<T>(T payload, MQPublishOptions options);
}
