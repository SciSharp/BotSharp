using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.Interfaces;
using BotSharp.Plugin.Twilio.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
}
