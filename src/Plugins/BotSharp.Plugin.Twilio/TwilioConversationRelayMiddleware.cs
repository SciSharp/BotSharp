using BotSharp.Plugin.Twilio.Models;
using BotSharp.Plugin.Twilio.Services;
using Microsoft.AspNetCore.Http;
using Twilio.Security;
using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio;

/// <summary>
/// Accepts Twilio ConversationRelay WebSockets at /twilio/relay/{agentId}/{conversationId}
/// </summary>
public class TwilioConversationRelayMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TwilioConversationRelayMiddleware> _logger;

    public TwilioConversationRelayMiddleware(RequestDelegate next, ILogger<TwilioConversationRelayMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        if (!httpContext.Request.Path.StartsWithSegments("/twilio/relay", out var rest)
            || !httpContext.WebSockets.IsWebSocketRequest)
        {
            await _next(httpContext);
            return;
        }

        var parts = (rest.Value ?? string.Empty).Trim('/').Split('/');
        if (parts.Length != 2 || parts.Any(string.IsNullOrWhiteSpace))
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var (agentId, conversationId) = (parts[0], parts[1]);
        var services = httpContext.RequestServices;
        var settings = services.GetRequiredService<TwilioSetting>();
        if (settings.ConversationRelay.ValidateSignature && !IsSignatureValid(httpContext, agentId, conversationId))
        {
            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        using var socket = await httpContext.WebSockets.AcceptWebSocketAsync();
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        var session = new ConversationRelaySession(socket, agentId, conversationId, settings,
            (message, callbacks) => SendCallerMessageAsync(scopeFactory, message, callbacks),
            _logger);
        await session.RunAsync(httpContext.RequestAborted);
    }

    private static async Task<AssistantMessage?> SendCallerMessageAsync(IServiceScopeFactory scopeFactory, CallerMessage message, AgentTurnCallbacks callbacks)
    {
        using var scope = scopeFactory.CreateScope();
        return await TwilioConversationProcessor.SendCallerMessageAsync(scope.ServiceProvider, message, callbacks);
    }

    /// <summary>
    /// Twilio signs the WebSocket handshake with X-Twilio-Signature over the wss URL from the TwiML.
    /// </summary>
    private bool IsSignatureValid(HttpContext httpContext, string agentId, string conversationId)
    {
        var services = httpContext.RequestServices;
        var authToken = services.GetRequiredService<IConfiguration>()["Twilio:RequestValidation:AuthToken"];
        if (string.IsNullOrEmpty(authToken))
        {
            _logger.LogWarning("Twilio auth token is not configured; skipping ConversationRelay signature check.");
            return true;
        }

        var signature = httpContext.Request.Headers["X-Twilio-Signature"].ToString();
        var url = services.GetRequiredService<TwilioService>().GetConversationRelayUrl(agentId, conversationId);
        var valid = !string.IsNullOrEmpty(signature)
            && new RequestValidator(authToken).Validate(url, new Dictionary<string, string>(), signature);
        if (!valid)
        {
            _logger.LogWarning($"Rejected ConversationRelay connection for conversation {conversationId}: invalid or missing X-Twilio-Signature.");
        }
        return valid;
    }
}
