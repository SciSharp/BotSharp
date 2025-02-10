using BotSharp.Abstraction.Infrastructures;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.Interfaces;
using BotSharp.Plugin.Twilio.Models;
using BotSharp.Plugin.Twilio.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Twilio.TwiML.Voice;
using Conversation = BotSharp.Abstraction.Conversations.Models.Conversation;
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
            // SpeechPaths = ["twilio/welcome.mp3"],
            ActionOnEmptyResult = true
        };

        if (_context.HttpContext.Request.Query.ContainsKey("conversation_id"))
        {
            request.ConversationId = _context.HttpContext.Request.Query["conversation_id"];
        }
        else
        {
            request.ConversationId = request.CallSid;
        }

        await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
        {
            await hook.OnSessionCreating(request, instruction);
        }, new HookEmitOption
        {
            OnlyOnce = true
        });

        await InitConversation(request);

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

    private async Task InitConversation(ConversationalVoiceRequest request)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        var conversation = await convService.GetConversation(request.ConversationId);
        if (conversation != null)
        {
            return;
        }

        var states = new List<MessageState>
            {
                new("channel", ConversationChannel.Phone),
                new("calling_phone", request.From)
            };

        var conv = new Conversation
        {
            Id = request.CallSid,
            AgentId = _settings.AgentId,
            Channel = ConversationChannel.Phone,
            Title = $"Phone call from {request.From}",
            Tags = [],
        };

        conv = await convService.NewConversation(conv);
        convService.SetConversationId(conv.Id, states);
        convService.SaveStates();
    }
}
