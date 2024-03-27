using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using BotSharp.Plugin.Twilio.Services;
using BotSharp.Abstraction.Routing;

namespace BotSharp.Plugin.Twilio.Controllers;

[AllowAnonymous]
public class TwilioVoiceController : TwilioController
{
    private readonly TwilioSetting _settings;
    private readonly IServiceProvider _services;

    public TwilioVoiceController(TwilioSetting settings, IServiceProvider services)
    {
        _settings = settings;
        _services = services;
    }

    [Authorize]
    [HttpGet("/twilio/token")]
    public Token GetAccessToken()
    {
        var twilio = _services.GetRequiredService<TwilioService>();
        var accessToken = twilio.GetAccessToken();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        return new Token
        {
            AccessToken = accessToken,
            ExpireTime = jwt.Payload.Exp.Value,
            TokenType = "Bearer",
            Scope = "api"
        };
    }

    [HttpPost("/twilio/voice/welcome")]
    public async Task<TwiMLResult> StartConversation(VoiceRequest request)
    {
        string sessionId = $"TwilioVoice_{request.CallSid}";
        var twilio = _services.GetRequiredService<TwilioService>();
        var response = twilio.ReturnInstructions("Hello, how may I help you?");
        return TwiML(response);
    }

    [HttpPost("/twilio/voice/{agentId}")]
    public async Task<TwiMLResult> ReceivedVoiceMessage([FromRoute] string agentId, VoiceRequest input)
    {
        string sessionId = $"TwilioVoice_{input.CallSid}";

        var inputMsg = new RoleDialogModel(AgentRole.User, input.SpeechResult);
        var conv = _services.GetRequiredService<IConversationService>();
        var routing = _services.GetRequiredService<IRoutingService>();
        routing.Context.SetMessageId(sessionId, inputMsg.MessageId);

        conv.SetConversationId(sessionId, new List<MessageState>
        {
            new MessageState("channel", ConversationChannel.Phone),
            new MessageState("calling_phone", input.DialCallSid)
        });

        var twilio = _services.GetRequiredService<TwilioService>();
        VoiceResponse response = default;

        var result = await conv.SendMessage(agentId,
            inputMsg,
            replyMessage: null,
            async msg =>
            {
                response = twilio.ReturnInstructions(msg.Content);
                if (msg.FunctionName == "conversation_end")
                {
                    response = twilio.HangUp(msg.Content);
                }
            }, async functionExecuting =>
            {
            }, async functionExecuted =>
            {
            });

        return TwiML(response);
    }
}
