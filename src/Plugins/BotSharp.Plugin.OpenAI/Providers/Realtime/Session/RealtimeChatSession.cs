using BotSharp.Plugin.OpenAI.Models.Realtime;
using System;
using System.ClientModel;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

namespace BotSharp.Plugin.OpenAI.Providers.Realtime.Session;

public class RealtimeChatSession : IDisposable
{
    private readonly IServiceProvider _services;
    private readonly BotSharpOptions _options;

    private ClientWebSocket _webSocket;
    private readonly object _singleReceiveLock = new();
    private readonly SemaphoreSlim _clientSendSemaphore = new(initialCount: 1, maxCount: 1);
    private AsyncWebsocketDataCollectionResult _receivedCollectionResult;

    public RealtimeChatSession(
        IServiceProvider services,
        BotSharpOptions options)
    {
        _services = services;
        _options = options;
    }

    public async Task StartAsync(string provider, string model)
    {
        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(provider, model);

        _webSocket?.Dispose();
        _webSocket = new ClientWebSocket();
        _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {settings.ApiKey}");
        _webSocket.Options.SetRequestHeader("OpenAI-Beta", "realtime=v1");

        await _webSocket.ConnectAsync(new Uri($"wss://api.openai.com/v1/realtime?model={model}"), CancellationToken.None);
    }

    public async IAsyncEnumerable<SessionConversationUpdate> ReceiveUpdatesAsync()
    {
        await foreach (ClientResult result in ReceiveInnerUpdatesAsync())
        {
            var update = HandleSessionResult(result);
            yield return update;
        }
    }

    public async IAsyncEnumerable<ClientResult> ReceiveInnerUpdatesAsync()
    {
        lock (_singleReceiveLock)
        {
            _receivedCollectionResult ??= new(_webSocket);
        }

        await foreach (var result in _receivedCollectionResult)
        {
            yield return result;
        }
    }

    private SessionConversationUpdate HandleSessionResult(ClientResult result)
    {
        using var response = result.GetRawResponse();
        var bytes = response.Content.ToArray();
        var text = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        return new SessionConversationUpdate
        {
            RawResponse = text
        };
    }

    public async Task SendEventToModel(object message)
    {
        if (_webSocket.State != WebSocketState.Open)
        {
            return;
        }

        await _clientSendSemaphore.WaitAsync().ConfigureAwait(false);

        try
        {
            if (message is not string data)
            {
                data = JsonSerializer.Serialize(message, _options.JsonSerializerOptions);
            }

            var buffer = Encoding.UTF8.GetBytes(data);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        finally
        {
            _clientSendSemaphore.Release();
        }
    }

    public async Task Disconnect()
    {
        if (_webSocket.State == WebSocketState.Open)
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
        }
    }

    public void Dispose()
    {
        _webSocket?.Dispose();
    }
}
