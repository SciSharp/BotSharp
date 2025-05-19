using System.ClientModel;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;

namespace BotSharp.Core.Session;

public class LlmRealtimeSession : IDisposable
{
    private readonly IServiceProvider _services;
    private readonly ChatSessionOptions? _sessionOptions;

    private ClientWebSocket _webSocket;
    private readonly object _singleReceiveLock = new();
    private readonly SemaphoreSlim _clientEventSemaphore = new(initialCount: 1, maxCount: 1);
    private AsyncWebsocketDataCollectionResult _receivedCollectionResult;
    private bool _disposed = false;

    public LlmRealtimeSession(
        IServiceProvider services,
        ChatSessionOptions? sessionOptions = null)
    {
        _services = services;
        _sessionOptions = sessionOptions;
    }

    public async Task ConnectAsync(Uri uri, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        _disposed = false;
        _webSocket?.Dispose();
        _webSocket = new ClientWebSocket();

        if (!headers.IsNullOrEmpty())
        {
            foreach (var header in headers)
            {
                _webSocket.Options.SetRequestHeader(header.Key, header.Value);
            }
        }

        await _webSocket.ConnectAsync(uri, cancellationToken);
    }

    public async IAsyncEnumerable<ChatSessionUpdate> ReceiveUpdatesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (ClientResult result in ReceiveInnerUpdatesAsync(cancellationToken))
        {
            var update = HandleSessionResult(result);
            yield return update;
        }
    }

    private async IAsyncEnumerable<ClientResult> ReceiveInnerUpdatesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        lock (_singleReceiveLock)
        {
            _receivedCollectionResult ??= new(_webSocket, _sessionOptions, cancellationToken);
        }

        await foreach (var result in _receivedCollectionResult)
        {
            yield return result;
        }
    }

    private ChatSessionUpdate HandleSessionResult(ClientResult result)
    {
        using var response = result.GetRawResponse();
        var bytes = response.Content.ToArray();
        var text = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        return new ChatSessionUpdate
        {
            RawResponse = text
        };
    }

    public async Task SendEventToModelAsync(object message)
    {
        try
        {
            if (_disposed)
            {
                return;
            }

            await _clientEventSemaphore.WaitAsync();

            if (_webSocket.State != WebSocketState.Open)
            {
                return;
            }

            if (message is not string data)
            {
                data = JsonSerializer.Serialize(message, _sessionOptions?.JsonOptions);
            }

            var buffer = Encoding.UTF8.GetBytes(data);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        finally
        {
            if (!_disposed)
            {
                _clientEventSemaphore.Release();
            }
        }
    }

    public async Task DisconnectAsync()
    {
        if (_disposed) return;

        if (_webSocket.State == WebSocketState.Open)
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _clientEventSemaphore?.Dispose();
        _webSocket?.Dispose();
    }
}
