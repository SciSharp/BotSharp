namespace BotSharp.Plugin.MessageQueue.Interfaces;

public interface IMQService
{
    /// <summary>
    /// Subscribe consumer
    /// </summary>
    /// <param name="key"></param>
    /// <param name="consumer"></param>
    void Subscribe(string key, object consumer);

    /// <summary>
    /// Publish payload to message queue
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="payload"></param>
    /// <param name="exchange"></param>
    /// <param name="routingkey"></param>
    /// <param name="milliseconds"></param>
    /// <param name="messageId"></param>
    /// <returns></returns>
    Task<bool> PublishAsync<T>(T payload, string exchange, string routingkey, long milliseconds = 0, string messageId = "");
}
