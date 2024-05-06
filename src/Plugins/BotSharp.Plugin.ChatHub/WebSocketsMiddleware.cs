using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace BotSharp.Plugin.ChatHub;

public class WebSocketsMiddleware
{
    private readonly RequestDelegate _next;

    public WebSocketsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        var request = httpContext.Request;;
        var messageFileRegex = new Regex(@"/conversation/[a-z0-9_.-]+/message/[a-z0-9_.-]+/file/[a-z0-9_.-]+/type/[a-z0-9_.-]+", RegexOptions.IgnoreCase);

        // web sockets cannot pass headers so we must take the access token from query param and
        // add it to the header before authentication middleware runs
        if ((request.Path.StartsWithSegments("/chatHub", StringComparison.OrdinalIgnoreCase) || messageFileRegex.IsMatch(request.Path.Value ?? string.Empty)) &&
            request.Query.TryGetValue("access_token", out var accessToken))
        {
            request.Headers["Authorization"] = $"Bearer {accessToken}";
        }

        await _next(httpContext);
    }
}
