using BotSharp.Core.Realtime.Models.Options;
using System.ClientModel;

namespace BotSharp.Core.Realtime.Websocket.Common;

internal class AsyncWebsocketDataCollectionResult : AsyncCollectionResult<ClientResult>
{
    private readonly WebSocket _webSocket;
    private readonly ChatSessionOptions? _sessionOptions;
    private readonly CancellationToken _cancellationToken;

    public AsyncWebsocketDataCollectionResult(
        WebSocket webSocket,
        ChatSessionOptions? sessionOptions,
        CancellationToken cancellationToken)
    {
        _webSocket = webSocket;
        _sessionOptions = sessionOptions;
        _cancellationToken = cancellationToken;
    }

    public override ContinuationToken? GetContinuationToken(ClientResult page)
    {
        return null;
    }

    public override async IAsyncEnumerable<ClientResult> GetRawPagesAsync()
    {
        await using var enumerator = new AsyncWebsocketDataResultEnumerator(_webSocket, _sessionOptions, _cancellationToken);
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
