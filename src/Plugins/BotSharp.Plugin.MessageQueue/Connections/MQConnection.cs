using BotSharp.Plugin.MessageQueue.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.IO;
using System.Threading;

namespace BotSharp.Plugin.MessageQueue.Connections;

public class MQConnection : IMQConnection
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger<MQConnection> _logger;

    private IConnection _connection;
    private bool _disposed;

    public MQConnection(
        MessageQueueSettings settings,
        ILogger<MQConnection> logger)
    {
        _logger = logger;
        _connectionFactory = new ConnectionFactory
        {
            HostName = settings.HostName,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password,
            VirtualHost = settings.VirtualHost,
            ConsumerDispatchConcurrency = 1,
            //DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            HandshakeContinuationTimeout = TimeSpan.FromSeconds(20)
        };
    }

    public bool IsConnected
    {
        get
        {
            return _connection != null && _connection.IsOpen && !_disposed;
        }
    }

    public IConnection Connection => _connection;

    public async Task<IChannel> CreateChannelAsync()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Rabbit MQ is not connectioned.");
        }
        return await _connection.CreateChannelAsync();
    }

    public async Task<bool> TryConnectAsync()
    {
        _lock.Wait();

        if (IsConnected)
        {
            return true;
        }

        _connection = await _connectionFactory.CreateConnectionAsync();
        if (IsConnected)
        {
            _connection.ConnectionShutdownAsync += OnConnectionShutdownAsync;
            _connection.CallbackExceptionAsync += OnCallbackExceptionAsync;
            _connection.ConnectionBlockedAsync += OnConnectionBlockedAsync;
            _logger.LogInformation($"Rabbit MQ client connection success. host: {_connection.Endpoint.HostName}, port: {_connection.Endpoint.Port}, localPort:{_connection.LocalPort}");
            return true;
        }
        _logger.LogError("Rabbit MQ client connection error.");
        return false;
    }

    private Task OnConnectionShutdownAsync(object sender, ShutdownEventArgs e)
    {
        if (_disposed)
        {
            return Task.CompletedTask;
        }

        _logger.LogError($"Rabbit MQ connection is on shutdown. Trying to reconnect, {e.ReplyCode}:{e.ReplyText}.");
        return Task.CompletedTask;
    }

    private Task OnCallbackExceptionAsync(object sender, CallbackExceptionEventArgs e)
    {
        if (_disposed)
        {
            return Task.CompletedTask;
        }

        _logger.LogError($"Rabbit MQ connection throw exception. Trying to reconnect, {e.Exception}.");
        return Task.CompletedTask;
    }

    private Task OnConnectionBlockedAsync(object sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed)
        {
            return Task.CompletedTask;
        }

        _logger.LogError($"Rabbit MQ connection is shutdown. Trying to reconnect, {e.Reason}.");
        return Task.CompletedTask;
    }


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        _logger.LogWarning("Disposing Rabbit MQ connection.");
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            _connection.Dispose();
            _logger.LogWarning("Disposed Rabbit MQ connection.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }
}
