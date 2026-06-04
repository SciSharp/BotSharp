using BotSharp.Abstraction.Conversations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading;

namespace BotSharp.Plugin.Membase.Handlers;

public class MembaseAuthHandler : DelegatingHandler
{
    private const string TokenStateKey = "membase_access_token";

    private readonly MembaseSettings _settings;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<MembaseAuthHandler> _logger;

    public MembaseAuthHandler(
        ILogger<MembaseAuthHandler> logger,
        MembaseSettings settings,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _settings = settings;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
    {
        var token = ResolveToken();
        requestMessage.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
        var response = await base.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);

#if DEBUG
        var rawResponse = await response.Content.ReadAsStringAsync();
        _logger.LogDebug(rawResponse);
#endif

        return response;
    }

    private string ResolveToken()
    {
        var requestServices = _httpContextAccessor.HttpContext?.RequestServices;
        if (requestServices != null)
        {
            var state = requestServices.GetService<IConversationStateService>();
            var stateToken = state?.GetState(TokenStateKey);
            if (!string.IsNullOrWhiteSpace(stateToken))
            {
                return stateToken;
            }
        }

        return _settings.ApiKey;
    }
}
