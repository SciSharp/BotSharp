using BotSharp.Abstraction.Infrastructures;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.Interfaces;
using BotSharp.Plugin.Twilio.Models;
using BotSharp.Plugin.Twilio.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BotSharp.Plugin.Twilio.Controllers;

public class TwilioOutboundController : TwilioController
{
    private readonly TwilioSetting _settings;
    private readonly IServiceProvider _services;
    private readonly IHttpContextAccessor _context;
    private readonly ILogger _logger;

    public TwilioOutboundController(TwilioSetting settings, IServiceProvider services, IHttpContextAccessor context, ILogger<TwilioOutboundController> logger)
    {
        _settings = settings;
        _services = services;
        _context = context;
        _logger = logger;
    }

    [ValidateRequest]
    [HttpPost("twilio/voice/init-outbound-call")]
    public async Task<TwiMLResult> InitiateOutboundCall(ConversationalVoiceRequest request)
    {
        var twilio = _services.GetRequiredService<TwilioService>();

        VoiceResponse response = default!;
        if (twilio.MachineDetected(request))
        {
            response = new VoiceResponse();
            // Authenticate (synchronous, so identity writes survive into the async status hook)
            HookEmitter.Emit<ITwilioCallStatusHook>(_services, hook => hook.OnAuthenticate(request), request.AgentId);
            await HookEmitter.Emit<ITwilioCallStatusHook>(_services,
                async hook => await hook.OnVoicemailStarting(request), request.AgentId);

            var voicemail = twilio.GetSavedSpeechFile(request.ConversationId, "voicemail");
            twilio.AppendSpeech(response, request.ConversationId, voicemail);
        }
        else if (_settings.ConversationRelay.IsEnabledFor(request.AgentId))
        {
            // OutboundPhoneCallFn stored the initial message as reply 0
            var sessionManager = _services.GetRequiredService<ITwilioSessionManager>();
            var initial = await sessionManager.GetAssistantReplyAsync(request.ConversationId, 0);
            if (string.IsNullOrWhiteSpace(initial?.Content))
            {
                _logger.LogWarning($"No initial message for conversation {request.ConversationId}; starting ConversationRelay without a greeting.");
            }
            response = twilio.ReturnConversationRelay(request.AgentId, request.ConversationId, initial?.Content);
        }
        else
        {
            var instruction = new ConversationalVoiceResponse
            {
                AgentId = request.AgentId,
                ConversationId = request.ConversationId,
                ActionOnEmptyResult = true,
                CallbackPath = $"twilio/voice/receive/1?agent-id={request.AgentId}&conversation-id={request.ConversationId}",
            };

            if (request.InitAudioFile != null)
            {
                instruction.SpeechPaths.Add(request.InitAudioFile);
            }
            response = twilio.ReturnNoninterruptedInstructions(instruction);
        }

        return TwiML(response);
    }
}
