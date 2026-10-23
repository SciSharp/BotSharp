using BotSharp.Abstraction.Infrastructures;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.Interfaces;
using BotSharp.Plugin.Twilio.Models.Relay;
using BotSharp.Plugin.Twilio.Services;
using Microsoft.AspNetCore.Mvc;

namespace BotSharp.Plugin.Twilio.Controllers;

public class TwilioConversationRelayController : TwilioController
{
    private readonly TwilioSetting _settings;
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public TwilioConversationRelayController(
        TwilioSetting settings,
        IServiceProvider services,
        ILogger<TwilioConversationRelayController> logger)
    {
        _settings = settings;
        _services = services;
        _logger = logger;
    }

    /// <summary>
    /// Twilio calls this when a ConversationRelay session ends: we sent "end" (handoff), the caller hung up,
    /// or the session failed.
    /// </summary>
    [ValidateRequest]
    [HttpPost("twilio/relay/action")]
    public async Task<TwiMLResult> SessionEnded(ConversationRelayActionRequest request)
    {
        _logger.LogInformation($"ConversationRelay session for conversation {request.ConversationId} ended: " +
            $"{request.SessionStatus} {request.ErrorCode} {request.ErrorMessage}");

        // Authenticate (synchronous, so identity writes survive into the async session hooks)
        HookEmitter.Emit<ITwilioSessionHook>(_services, hook => hook.OnAuthenticate(request), request.AgentId);

        var twilio = _services.GetRequiredService<TwilioService>();
        var action = GetHandoffAction(request.HandoffData);

        if (action == ConversationRelayHandoff.Transfer && !string.IsNullOrEmpty(_settings.CsrAgentNumber))
        {
            await HookEmitter.Emit<ITwilioSessionHook>(_services,
                async hook => await hook.OnAgentTransferring(request, _settings), request.AgentId);
            return TwiML(twilio.DialCsrAgent(string.Empty));
        }

        var callStillUp = !string.Equals(request.CallStatus, "completed", StringComparison.OrdinalIgnoreCase);
        var canReconnect = request.Reconnects < _settings.ConversationRelay.MaxReconnects;
        if (request.SessionStatus == "failed" && callStillUp && canReconnect)
        {
            _logger.LogWarning($"Reconnecting ConversationRelay for conversation {request.ConversationId} " +
                $"(attempt {request.Reconnects + 1}).");
            var relay = twilio.ReturnConversationRelay(request.AgentId, request.ConversationId,
                welcomeGreeting: null, request.Reconnects + 1);
            return TwiML(relay);
        }

        if (action == ConversationRelayHandoff.HangUp)
        {
            await HookEmitter.Emit<ITwilioSessionHook>(_services,
                async hook => await hook.OnAgentHangUp(request), request.AgentId);
        }

        var response = new VoiceResponse();
        response.Hangup();
        return TwiML(response);
    }

    private static string? GetHandoffAction(string? handoffData)
    {
        if (string.IsNullOrWhiteSpace(handoffData))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(handoffData);
            return doc.RootElement.TryGetProperty("action", out var action) ? action.GetString() : null;
        }
        catch (JsonException)
        {
            return handoffData;
        }
    }
}
