using BotSharp.Abstraction.Agents.Models;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.Interfaces;
using BotSharp.Plugin.Twilio.Models;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IO;

namespace BotSharp.Plugin.Twilio.Controllers;

public class TwilioRecordController : TwilioController
{
    private readonly TwilioSetting _settings;
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public TwilioRecordController(TwilioSetting settings, IServiceProvider services, IHttpContextAccessor context, ILogger<TwilioRecordController> logger)
    {
        _settings = settings;
        _services = services;
        _logger = logger;
    }

    [ValidateRequest]
    [HttpPost("twilio/record/status")]
    public async Task<ActionResult> PhoneRecordingStatus(ConversationalVoiceRequest request)
    {
        if (request.RecordingStatus == "completed")
        {
            _logger.LogInformation($"Recording completed for {request.CallSid}, the record URL is {request.RecordingUrl}");

            // Set the recording URL to the conversation state
            var convService = _services.GetRequiredService<IConversationService>();
            convService.SetConversationId(request.ConversationId, new List<MessageState>
            {
                new("phone_recording_url", request.RecordingUrl)
            });
            convService.SaveStates();

            // recording completed
            await HookEmitter.Emit<ITwilioCallStatusHook>(_services, x => x.OnRecordingCompleted(request));
        }

        return Ok();
    }

    [ValidateRequest]
    [HttpPost("twilio/record/transcribe")]
    public async Task<ActionResult> PhoneRecordingTranscribe(ConversationalVoiceRequest request)
    {
        if (request.Final == "true")
        {
            _logger.LogError($"Transcription completed for {request.CallSid}, the transcription is: {request.TranscriptionData}");

            // transcription completed
            await HookEmitter.Emit<ITwilioCallStatusHook>(_services, x => x.OnTranscribeCompleted(request));

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
            }
        }

        return Ok();
    }
}
