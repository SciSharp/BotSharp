using System.Collections.Concurrent;

namespace BotSharp.Plugin.RabbitMQ.Connections;

public static class RabbitMQChannelPoolFactory
{
    private static readonly ConcurrentDictionary<string, RabbitMQChannelPool> _poolDict = new();

    public static RabbitMQChannelPool GetChannelPool(IServiceProvider services, IRabbitMQConnection rabbitMQConnection)
    {
        return _poolDict.GetOrAdd(rabbitMQConnection.Connection.ToString()!, key => new RabbitMQChannelPool(services, rabbitMQConnection));
    }
}
