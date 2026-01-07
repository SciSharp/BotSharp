using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading;

namespace BotSharp.Plugin.Membase.Services;

public class MembaseAuthHandler : DelegatingHandler
{
    private readonly MembaseSettings _settings;
    private readonly ILogger<MembaseAuthHandler> _logger;

    public MembaseAuthHandler(
        ILogger<MembaseAuthHandler> logger,
        MembaseSettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
    {
        requestMessage.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_settings.ApiKey}");
        var response = await base.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);

#if DEBUG
        var rawResponse = await response.Content.ReadAsStringAsync();
        _logger.LogDebug(rawResponse);
#endif

        return response;
    }
}
