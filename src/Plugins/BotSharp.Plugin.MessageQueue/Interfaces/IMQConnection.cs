using RabbitMQ.Client;

namespace BotSharp.Plugin.MessageQueue.Interfaces;

public interface IMQConnection : IDisposable
{
    bool IsConnected { get; }
    Task<IChannel> CreateChannelAsync();
    Task<bool> ConnectAsync();
}
