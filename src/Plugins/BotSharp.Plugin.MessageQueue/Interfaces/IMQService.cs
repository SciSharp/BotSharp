namespace BotSharp.Plugin.MessageQueue.Interfaces;

public interface IMQService
{
    Task<bool> PublishAsync<T>(T payload, string exchange, string routingkey, long milliseconds = 0, string messageId = "");
    Task SubscribeAsync(string key, object consumer);
}
