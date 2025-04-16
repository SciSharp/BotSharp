using System;
using System.Buffers;
using System.ClientModel;
using System.Net.WebSockets;

namespace BotSharp.Plugin.OpenAI.Providers.Realtime.Session;

public class AsyncWebsocketDataResultEnumerator : IAsyncEnumerator<ClientResult>
{
    private readonly WebSocket _webSocket;
    private readonly CancellationToken _cancellationToken;
    private readonly byte[] _buffer;

    public AsyncWebsocketDataResultEnumerator(
        WebSocket webSocket,
        CancellationToken cancellationToken)
    {
        _webSocket = webSocket;
        _cancellationToken = cancellationToken;
        _buffer = ArrayPool<byte>.Shared.Rent(1024 * 32);
    }

    public ClientResult Current { get; private set; }

    public ValueTask DisposeAsync()
    {
        _webSocket?.Dispose();
        return new ValueTask(Task.CompletedTask);
    }

    public async ValueTask<bool> MoveNextAsync()
    {
        var response = new AiWebsocketPipelineResponse();
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
