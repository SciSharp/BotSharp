using BotSharp.Plugin.Twilio.Models;
using BotSharp.Plugin.Twilio.Services;
using Microsoft.AspNetCore.Mvc;

namespace BotSharp.Plugin.Twilio.Controllers;

public class TwilioReconnectController : TwilioController
{
    private readonly TwilioSetting _settings;
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public TwilioReconnectController(IServiceProvider services, TwilioSetting settings, ILogger<TwilioReconnectController> logger)
    {
        _services = services;
        _settings = settings;
        _logger = logger;
    }

    [ValidateRequest]
    [HttpPost("twilio/stream/reconnect")]
    public async Task<TwiMLResult> Reconnect(ConversationalVoiceRequest request)
    {
        var response = new VoiceResponse();
        var connect = new Connect();
        var host = _settings.CallbackHost.Split("://").Last();
        connect.Stream(url: $"wss://{host}/twilio/stream/{request.AgentId}/{request.ConversationId}");
        if (!string.IsNullOrEmpty(request.InitAudioFile))
        {
            var twilio = _services.GetRequiredService<TwilioService>();
            var audioUrl = twilio.GetSpeechPath(request.ConversationId, request.InitAudioFile);
            response.Play(new Uri(audioUrl));
        }
        else
        {
            // Leave a pause to allow disposing objects.
            response.Pause(1);
        }
        response.Append(connect);
        return TwiML(response);
    }
}
