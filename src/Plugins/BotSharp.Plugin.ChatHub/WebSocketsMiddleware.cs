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
        var request = httpContext.Request;

        // web sockets cannot pass headers so we must take the access token from query param and
        // add it to the header before authentication middleware runs
        if ((VerifyChatHubRequest(request) || VerifyGetRequest(request)) &&
            request.Query.TryGetValue("access_token", out var accessToken))
        {
            request.Headers["Authorization"] = $"Bearer {accessToken}";
        }

        await _next(httpContext);
    }

    private bool VerifyChatHubRequest(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/chatHub", StringComparison.OrdinalIgnoreCase);
    }

    private bool VerifyGetRequest(HttpRequest request)
    {
        var regexes = new List<Regex>
        {
            new Regex(@"/conversation/(.*?)/message/(.*?)/(.*?)/file/(.*?)/(.*?)", RegexOptions.IgnoreCase),
            new Regex(@"/user/avatar", RegexOptions.IgnoreCase),
            new Regex(@"/knowledge/document/(.*?)/file/(.*?)", RegexOptions.IgnoreCase)
        };

        return request.Method.IsEqualTo("GET") && regexes.Any(x => x.IsMatch(request.Path.Value ?? string.Empty));
    }
}
