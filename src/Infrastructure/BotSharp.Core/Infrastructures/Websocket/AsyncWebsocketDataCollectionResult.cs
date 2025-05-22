using System.ClientModel;
using System.Net.WebSockets;

namespace BotSharp.Core.Infrastructures.Websocket;

internal class AsyncWebsocketDataCollectionResult : AsyncCollectionResult<ClientResult>
{
    private readonly WebSocket _webSocket;
    private readonly ChatSessionOptions? _options;
    private readonly CancellationToken _cancellationToken;

    public AsyncWebsocketDataCollectionResult(
        WebSocket webSocket,
        ChatSessionOptions? options,
        CancellationToken cancellationToken)
    {
        _webSocket = webSocket;
        _options = options;
        _cancellationToken = cancellationToken;
    }

    public override ContinuationToken? GetContinuationToken(ClientResult page)
    {
        return null;
    }

    public override async IAsyncEnumerable<ClientResult> GetRawPagesAsync()
    {
        await using var enumerator = new AsyncWebsocketDataResultEnumerator(_webSocket, _options, _cancellationToken);
        while (await enumerator.MoveNextAsync().ConfigureAwait(false))
        {
            yield return enumerator.Current;
        }
    }

    protected override async IAsyncEnumerable<ClientResult> GetValuesFromPageAsync(ClientResult page)
    {
        await Task.CompletedTask;
        yield return page;
    }
}
