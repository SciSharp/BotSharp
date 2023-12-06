using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using BotSharp.Plugin.Twilio.Services;

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

        var conv = _services.GetRequiredService<IConversationService>();
        conv.SetConversationId(sessionId, new List<string>
        {
            $"channel={ConversationChannel.Phone}",
            $"calling_phone={input.DialCallSid}"
        });

        var twilio = _services.GetRequiredService<TwilioService>();
        VoiceResponse response = default;

        var result = await conv.SendMessage(agentId,
            new RoleDialogModel(AgentRole.User, input.SpeechResult),
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
