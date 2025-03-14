using BotSharp.Abstraction.Infrastructures;
using BotSharp.Abstraction.Infrastructures.Enums;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.Interfaces;
using BotSharp.Plugin.Twilio.Models;
using BotSharp.Plugin.Twilio.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Conversation = BotSharp.Abstraction.Conversations.Models.Conversation;

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

        VoiceResponse response = default!;

        if (request.AnsweredBy == "machine_start" &&
            request.Direction == "outbound-api" &&
            request.InitAudioFile != null)
        {
            response = new VoiceResponse();
            response.Play(new Uri(request.InitAudioFile));
            return TwiML(response);
        }

        var instruction = new ConversationalVoiceResponse
        {
            SpeechPaths = [],
            ActionOnEmptyResult = true
        };

        if (request.InitAudioFile != null)
        {
            instruction.SpeechPaths.Add($"twilio/voice/speeches/{request.ConversationId}/{request.InitAudioFile}");
        }

        await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
        {
            await hook.OnSessionCreating(request, instruction);
        }, new HookEmitOption
        {
            OnlyOnce = true
        });

        request.ConversationId = await InitConversation(request);

        var twilio = _services.GetRequiredService<TwilioService>();

        response = twilio.ReturnBidirectionalMediaStreamsInstructions(request.ConversationId, instruction);

        await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
        {
            await hook.OnSessionCreated(request);
        }, new HookEmitOption
        {
            OnlyOnce = true
        });

        return TwiML(response);
    }

    private async Task<string> InitConversation(ConversationalVoiceRequest request)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        var conversation = await convService.GetConversation(request.ConversationId);
        if (conversation == null)
        {
            var conv = new Conversation
            {
                AgentId = request.AgentId ?? _settings.AgentId,
                Channel = ConversationChannel.Phone,
                ChannelId = request.CallSid,
                Title = $"Incoming phone call from {request.From}",
                Tags = [],
            };

            conversation = await convService.NewConversation(conv);
        }

        var states = new List<MessageState>
            {
                new("channel", ConversationChannel.Phone),
                new("calling_phone", request.From),
                new("twilio_call_sid", request.CallSid),
                // Enable lazy routing mode to optimize realtime experience
                new(StateConst.ROUTING_MODE, "lazy"),
            };

        if (request.InitAudioFile != null)
        {
            states.Add(new("init_audio_file", request.InitAudioFile));
        }

        convService.SetConversationId(conversation.Id, states);
        convService.SaveStates();

        return conversation.Id;
    }
}
