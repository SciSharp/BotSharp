using BotSharp.Abstraction.Infrastructures;
using BotSharp.Abstraction.Realtime.Models;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.Interfaces;
using BotSharp.Plugin.Twilio.Models;
using BotSharp.Plugin.Twilio.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio.Controllers;

public class TwilioStreamController : TwilioController
{
    private readonly TwilioSetting _settings;
    private readonly IServiceProvider _services;
    private readonly IHttpContextAccessor _context;
    private readonly ILogger _logger;

    public TwilioStreamController(TwilioSetting settings, IServiceProvider services, IHttpContextAccessor context, ILogger<TwilioStreamController> logger)
    {
        _settings = settings;
        _services = services;
        _context = context;
        _logger = logger;
    }

    [ValidateRequest]
    [HttpPost("twilio/stream")]
    public async Task<TwiMLResult> InitiateStreamConversation(ConversationalVoiceRequest request)
    {
        var text = JsonSerializer.Serialize(request);
        if (request?.CallSid == null)
        {
            throw new ArgumentNullException(nameof(VoiceRequest.CallSid));
        }

        VoiceResponse response = null;
        var instruction = new ConversationalVoiceResponse
        {
            SpeechPaths = ["twilio/welcome.mp3"],
            ActionOnEmptyResult = true
        };
        await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
        {
            await hook.OnSessionCreating(request, instruction);
        }, new HookEmitOption
        {
            OnlyOnce = true
        });

        request.ConversationId = $"TwilioVoice_{request.CallSid}";

        var twilio = _services.GetRequiredService<TwilioService>();

        response = twilio.ReturnBidirectionalMediaStreamsInstructions(instruction);
        /*if (string.IsNullOrWhiteSpace(request.Intent))
        {
            response = twilio.ReturnNoninterruptedInstructions(instruction);
        }
        else
        {
            int seqNum = 0;
            var messageQueue = _services.GetRequiredService<TwilioMessageQueue>();
            var sessionManager = _services.GetRequiredService<ITwilioSessionManager>();
            await sessionManager.StageCallerMessageAsync(request.ConversationId, seqNum, request.Intent);
            var callerMessage = new CallerMessage()
            {
                ConversationId = request.ConversationId,
                SeqNumber = seqNum,
                Content = request.Intent,
                From = request.From,
                States = ParseStates(request.States)
            };
            await messageQueue.EnqueueAsync(callerMessage);
            response = new VoiceResponse();
            response.Redirect(new Uri($"{_settings.CallbackHost}/twilio/voice/{request.ConversationId}/reply/{seqNum}?{GenerateStatesParameter(request.States)}"), HttpMethod.Post);
        }*/

        /*await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
        {
            await hook.OnSessionCreated(request);
        }, new HookEmitOption
        {
            OnlyOnce = true
        });*/

        return TwiML(response);
    }
}
