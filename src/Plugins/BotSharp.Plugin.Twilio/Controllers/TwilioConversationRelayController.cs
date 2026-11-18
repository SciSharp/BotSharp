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
    private readonly ConversationRelaySessions _sessions;
    private readonly ILogger _logger;

    public TwilioConversationRelayController(
        TwilioSetting settings,
        IServiceProvider services,
        ConversationRelaySessions sessions,
        ILogger<TwilioConversationRelayController> logger)
    {
        _settings = settings;
        _services = services;
        _sessions = sessions;
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

        // The session's playback was stuck: a new session says what never finished playing. Nothing is told to the hooks:
        // the call goes on.
        if (action == ConversationRelayHandoff.Reconnect)
        {
            if (callStillUp && canReconnect)
            {
                _logger.LogWarning($"Reconnecting ConversationRelay for conversation {request.ConversationId}: its playback " +
                    $"was stuck (attempt {request.Reconnects + 1}).");
                return TwiML(twilio.ReturnConversationRelay(request.AgentId, request.ConversationId, welcomeGreeting: null,
                    request.Reconnects + 1, GetHandoffField(request.HandoffData, ConversationRelayHandoff.ResumeField)));
            }
            // The session asks only while it may (it knows its reconnects): a caller who hung up meanwhile ends the call
            // as any hang-up does, and anything else as the agent's
            action = callStillUp ? ConversationRelayHandoff.HangUp : null;
        }

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
        else if (action == null && !callStillUp)
        {
            // The WebSocket may still be finishing the turn the caller hung up on; it saves the conversation when it does
            var wait = TimeSpan.FromSeconds(_settings.ConversationRelay.CallerHangUpWaitSeconds);
            if (!string.IsNullOrEmpty(request.ConversationId)
                && !await _sessions.WaitForAsync(request.ConversationId, wait))
            {
                _logger.LogWarning($"ConversationRelay session for conversation {request.ConversationId} was still " +
                    $"running {wait.TotalSeconds}s after the caller hung up; its last turn may not be saved yet.");
            }

            await HookEmitter.Emit<ITwilioSessionHook>(_services,
                async hook => await hook.OnCallerHangUp(request), request.AgentId);
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

    /// <summary>
    /// A field of the handoffData as JSON, or null when it has none
    /// </summary>
    private static string? GetHandoffField(string? handoffData, string name)
    {
        if (string.IsNullOrWhiteSpace(handoffData))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(handoffData);
            return doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty(name, out var field)
                ? field.GetRawText()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
