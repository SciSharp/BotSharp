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

            await HookEmitter.Emit<ITwilioCallStatusHook>(_services, 
                async hook => await hook.OnVoicemailStarting(request));

            var url = twilio.GetSpeechPath(request.ConversationId, "voicemail.mp3");
            response.Play(new Uri(url));
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
