using RabbitMQ.Client;

namespace BotSharp.Plugin.MessageQueue.Interfaces;

public interface IMQConnection : IDisposable
{
    IConnection Connection { get; }
    bool IsConnected { get; }
    Task<IChannel> CreateChannelAsync();
    Task<bool> TryConnectAsync();
}
