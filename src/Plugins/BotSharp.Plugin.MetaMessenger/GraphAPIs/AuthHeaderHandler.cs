using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using BotSharp.Plugin.MetaMessenger.Settings;

namespace BotSharp.Plugin.MetaMessenger.GraphAPIs;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _context;
    private readonly MetaMessengerSetting _settings;

    public AuthHeaderHandler(IHttpContextAccessor context,
        MetaMessengerSetting settings)
    {
        _context = context;
        _settings = settings;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // request.Headers.Add("Authorization", _settings.PageAccessToken);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
