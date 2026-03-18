using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;

namespace BotSharp.Plugin.RabbitMQ.Connections;

public class RabbitMQChannelPool
{
    private readonly ObjectPool<IChannel> _pool;
    private readonly ILogger<RabbitMQChannelPool> _logger;
    private readonly int _tryLimit = 3;

    public RabbitMQChannelPool(
        IServiceProvider services,
        IRabbitMQConnection mqConnection)
    {
        _logger = services.GetRequiredService<ILoggerFactory>().CreateLogger<RabbitMQChannelPool>();
        var poolProvider = new DefaultObjectPoolProvider();
        var policy = new ChannelPoolPolicy(mqConnection.Connection);
        _pool = poolProvider.Create(policy);
    }

    public IChannel Get()
    {
        var count = 0;
        var channel = _pool.Get();

        while (count < _tryLimit && channel.IsClosed)
        {
            channel.Dispose();
            channel = _pool.Get();
            count++;
        }

        if (channel.IsClosed)
        {
            _logger.LogWarning($"No open channel from the pool after {_tryLimit} retries.");
        }

        return channel;
    }

    public void Return(IChannel channel)
    {
        if (channel.IsOpen)
        {
            _pool.Return(channel);
        }
        else
        {
            channel.Dispose();
        }
    }
}

internal class ChannelPoolPolicy : IPooledObjectPolicy<IChannel>
{
    private readonly IConnection _connection;

    public ChannelPoolPolicy(IConnection connection)
    {
        _connection = connection;
    }

    public IChannel Create()
    {
        return _connection.CreateChannelAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public bool Return(IChannel obj)
    {
        return true;
    }
}