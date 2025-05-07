using BotSharp.Core.Realtime.Websocket.Common;
using System.ClientModel;
using System.Runtime.CompilerServices;

namespace BotSharp.Core.Realtime.Websocket.Chat;

public class BotSharpRealtimeSession : IDisposable
{
    private readonly IServiceProvider _services;
    private readonly WebSocket _websocket;
    private readonly ChatSessionOptions? _sessionOptions;
    private readonly object _singleReceiveLock = new();
    private AsyncWebsocketDataCollectionResult _receivedCollectionResult;

    public BotSharpRealtimeSession(
        IServiceProvider services,
        WebSocket websocket,
        ChatSessionOptions? sessionOptions)
    {
        _services = services;
        _websocket = websocket;
        _sessionOptions = sessionOptions;
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
            _receivedCollectionResult ??= new(_websocket, _sessionOptions, cancellationToken);
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

    public async Task SendEvent(string message)
    {
        if (_websocket.State == WebSocketState.Open)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await _websocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    public async Task Disconnect()
    {
        if (_websocket.State == WebSocketState.Open)
        {
            await _websocket.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
        }
    }

    public void Dispose()
    {
        _websocket.Dispose();
    }
}
