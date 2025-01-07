using BotSharp.Abstraction.Http;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace BotSharp.Plugin.HttpHandler.Hooks;

public class BasicHttpRequestHook : IHttpRequestHook
{
    private readonly IServiceProvider _services;
    private readonly IHttpContextAccessor _context;

    private const string AUTHORIZATION = "Authorization";
    private const string ORIGIN = "Origin";

    public BasicHttpRequestHook(
        IServiceProvider services,
        IHttpContextAccessor context)
    {
        _services = services;
        _context = context;
    }

    public void OnAddHttpHeaders(HttpHeaders headers)
    {
        var settings = _services.GetRequiredService<HttpHandlerSettings>();

        var auth = $"{_context.HttpContext.Request.Headers[AUTHORIZATION]}";
        if (!string.IsNullOrEmpty(auth))
        {
            headers.Add(AUTHORIZATION, auth);
        }

        var origin = $"{_context.HttpContext.Request.Headers[ORIGIN]}";
        origin = !string.IsNullOrEmpty(settings.Origin) ? settings.Origin : origin;
        if (!string.IsNullOrEmpty(origin))
        {
            headers.Add(ORIGIN, origin);
        }
    }
}
