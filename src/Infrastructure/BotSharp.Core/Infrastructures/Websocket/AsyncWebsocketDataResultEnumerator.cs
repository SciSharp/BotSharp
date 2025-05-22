using Microsoft.AspNetCore.Builder;
using System.Buffers;
using System.ClientModel;
using System.Net.WebSockets;

namespace BotSharp.Core.Infrastructures.Websocket;

internal class AsyncWebsocketDataResultEnumerator : IAsyncEnumerator<ClientResult>
{
    private readonly WebSocket _webSocket;
    private readonly ChatSessionOptions? _options;
    private readonly CancellationToken _cancellationToken;
    private readonly byte[] _buffer;

    private const int DEFAULT_BUFFER_SIZE = 1024 * 32;

    public AsyncWebsocketDataResultEnumerator(
        WebSocket webSocket,
        ChatSessionOptions? options,
        CancellationToken cancellationToken)
    {
        _webSocket = webSocket;
        _options = options;
        _cancellationToken = cancellationToken;
        var bufferSize = options?.BufferSize > 0 ? options.BufferSize.Value : DEFAULT_BUFFER_SIZE;
        _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
    }

    public ClientResult Current { get; private set; }

    public ValueTask DisposeAsync()
    {
        ArrayPool<byte>.Shared.Return(_buffer, clearArray: true);
        _webSocket?.Dispose();
        return new ValueTask(Task.CompletedTask);
    }

    public async ValueTask<bool> MoveNextAsync()
    {
        var response = new WebsocketPipelineResponse();
        while (!response.IsComplete)
        {
            var receivedResult = await _webSocket.ReceiveAsync(new(_buffer), _cancellationToken);

            if (receivedResult.CloseStatus.HasValue)
            {
#if DEBUG
                if (_options?.Logger != null)
                {
                    _options.Logger.LogWarning($"{_options?.Provider} Websocket close: ({receivedResult.CloseStatus}) {receivedResult.CloseStatusDescription}");
                }
#endif
                Current = null;
                return false;
            }

            var receivedBytes = _buffer.AsMemory(0, receivedResult.Count);
            var receivedData = BinaryData.FromBytes(receivedBytes);
            response.CollectReceivedResult(receivedResult, receivedData);
        }

        Current = ClientResult.FromResponse(response);
        return true;
    }
}
