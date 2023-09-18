using BotSharp.Plugin.HuggingFace.Settings;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

namespace BotSharp.Plugin.HuggingFace.Services;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly HuggingFaceSettings _settings;
    public AuthHeaderHandler(HuggingFaceSettings settings)
    {
        _settings = settings;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.Token);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
