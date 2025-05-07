using BotSharp.Core.Realtime.Models.Options;
using System.Buffers;
using System.ClientModel;

namespace BotSharp.Core.Realtime.Websocket.Common;

internal class AsyncWebsocketDataResultEnumerator : IAsyncEnumerator<ClientResult>
{
    private readonly WebSocket _webSocket;
    private readonly ChatSessionOptions? _sessionOptions;
    private readonly CancellationToken _cancellationToken;
    private readonly byte[] _buffer;

    private const int DEFAULT_BUFFER_SIZE = 1024 * 32;

    public AsyncWebsocketDataResultEnumerator(
        WebSocket webSocket,
        ChatSessionOptions? sessionOptions,
        CancellationToken cancellationToken)
    {
        _webSocket = webSocket;
        _sessionOptions = sessionOptions;
        _cancellationToken = cancellationToken;
        var bufferSize = sessionOptions?.BufferSize > 0 ? sessionOptions.BufferSize.Value : DEFAULT_BUFFER_SIZE;
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
