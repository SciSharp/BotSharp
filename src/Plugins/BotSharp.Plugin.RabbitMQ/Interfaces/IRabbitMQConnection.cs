using RabbitMQ.Client;

namespace BotSharp.Plugin.RabbitMQ.Interfaces;

public interface IRabbitMQConnection : IDisposable
{
    bool IsConnected { get; }
    IConnection Connection { get; }
    Task<IChannel> CreateChannelAsync();
    Task<bool> ConnectAsync();
}
