using BotSharp.Abstraction.Realtime;
using BotSharp.Abstraction.Routing;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.Interfaces;
using BotSharp.Plugin.Twilio.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace BotSharp.Plugin.Twilio.Controllers;

public class TwilioTranscribeController : TwilioController
{
    private readonly TwilioSetting _settings;
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public TwilioTranscribeController(TwilioSetting settings, IServiceProvider services, IHttpContextAccessor context, ILogger<TwilioRecordController> logger)
    {
        _settings = settings;
        _services = services;
        _logger = logger;
    }

    [ValidateRequest]
    [HttpPost("twilio/transcribe")]
    public async Task<ActionResult> PhoneRecordingTranscribe(ConversationalVoiceRequest request)
    {
        if (request.Final == "true")
        {
            _logger.LogInformation($"Transcription completed for {request.CallSid}, the transcription is: {request.TranscriptionData}");

            // Append the transcription to the dialog history
            var transcript = JsonConvert.DeserializeObject<TranscriptionData>(request.TranscriptionData);
            if (transcript != null && !string.IsNullOrEmpty(transcript.Transcript))
            {
                var storage = _services.GetRequiredService<IConversationStorage>();
                var message = new RoleDialogModel(AgentRole.User, transcript.Transcript)
                {
                    CurrentAgentId = request.AgentId
                };
                storage.Append(request.ConversationId, message);

                var routing = _services.GetRequiredService<IRoutingService>();
                routing.Context.SetMessageId(request.ConversationId, message.MessageId);

                var convService = _services.GetRequiredService<IConversationService>();
                convService.SetConversationId(request.ConversationId, []);

                // transcription completed
                transcript.Language = request.LanguageCode;
                await HookEmitter.Emit<IRealtimeHook>(_services, async x => await x.OnTranscribeCompleted(message, transcript));
            }
        }

        return Ok();
    }
}
