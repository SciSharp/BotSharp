using BotSharp.Plugin.MessageQueue.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.IO;
using System.Threading;

namespace BotSharp.Plugin.MessageQueue.Connections;

public class MQConnection : IMQConnection
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly SemaphoreSlim _lock = new(initialCount: 1, maxCount: 1);
    private readonly ILogger<MQConnection> _logger;

    private IConnection _connection;
    private bool _disposed = false;

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
            AutomaticRecoveryEnabled = true,
            HandshakeContinuationTimeout = TimeSpan.FromSeconds(20)
        };
    }

    public bool IsConnected => _connection != null && _connection.IsOpen && !_disposed;

    public async Task<IChannel> CreateChannelAsync()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Rabbit MQ is not connectioned.");
        }
        return await _connection.CreateChannelAsync();
    }

    public async Task<bool> ConnectAsync()
    {
        await _lock.WaitAsync();

        try
        {
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
        finally
        {
            _lock.Release();
        }
        
    }

    private Task OnConnectionShutdownAsync(object sender, ShutdownEventArgs e)
    {
        if (_disposed)
        {
            return Task.CompletedTask;
        }

        _logger.LogError($"Rabbit MQ connection is shutdown. {e}.");
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
        if (!disposing || _disposed)
        {
            return;
        }

        _logger.LogWarning("Start disposing Rabbit MQ connection.");

        try
        {
            _connection.Dispose();
            _disposed = true;
            _logger.LogWarning("Disposed Rabbit MQ connection.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when disposing Rabbit MQ connection");
        }
    }
}
