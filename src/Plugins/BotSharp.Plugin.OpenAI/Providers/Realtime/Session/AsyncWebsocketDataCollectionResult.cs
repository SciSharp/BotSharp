using BotSharp.Plugin.OpenAI.Models.Realtime;
using System.ClientModel;
using System.Net.WebSockets;

namespace BotSharp.Plugin.OpenAI.Providers.Realtime.Session;

public class AsyncWebsocketDataCollectionResult : AsyncCollectionResult<ClientResult>
{
    private readonly WebSocket _webSocket;

    public AsyncWebsocketDataCollectionResult(WebSocket webSocket)
    {
        _webSocket = webSocket;
    }

    public override ContinuationToken? GetContinuationToken(ClientResult page)
    {
        return null;
    }

    public override async IAsyncEnumerable<ClientResult> GetRawPagesAsync()
    {
        await using var enumerator = new AsyncWebsocketDataResultEnumerator(_webSocket);
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
