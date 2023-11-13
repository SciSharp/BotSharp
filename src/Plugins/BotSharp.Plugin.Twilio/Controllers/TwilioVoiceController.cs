using BotSharp.Abstraction.Conversations;
using BotSharp.Plugin.Twilio.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using Twilio.AspNet.Common;
using Twilio.AspNet.Core;
using Twilio.TwiML;
using Twilio.TwiML.Voice;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using BotSharp.Abstraction.Routing.Settings;
using Twilio.Http;
using Twilio.TwiML.Messaging;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Conversations.Models;

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
    
    [HttpPost("/twilio/voice/welcome")]
    public async Task<TwiMLResult> StartConversation(VoiceRequest request)
    {
        string sessionId = $"TwilioVoice_{request.CallSid}";
        var response = ReturnInstructions("Hello, how may I help you?");
        return TwiML(response);
    }

    [HttpPost("/twilio/voice/{agentId}")]
    public async Task<TwiMLResult> ReceivedVoiceMessage([FromRoute] string agentId, VoiceRequest input)
    {
        string sessionId = $"TwilioVoice_{input.CallSid}";

        var conv = _services.GetRequiredService<IConversationService>();
        conv.SetConversationId(sessionId, new List<string>
        {
            "channel=phone",
            $"calling_phone={input.DialCallSid}"
        });

        VoiceResponse response = default;

        var result = await conv.SendMessage(agentId, new RoleDialogModel(AgentRole.User, input.SpeechResult), async msg =>
        {
            response = HangUp(msg.Content);
        }, async functionExecuting =>
        {
        }, async functionExecuted =>
        {
        });

        return TwiML(response);
    }

    private VoiceResponse ReturnInstructions(string message)
    {
        var routingSetting = _services.GetRequiredService<RoutingSettings>();

        var response = new VoiceResponse();
        var gather = new Gather()
        {
            Input = new List<Gather.InputEnum>() 
            { 
                Gather.InputEnum.Speech 
            },
            Action = new Uri($"{_settings.CallbackHost}/twilio/voice/{routingSetting.RouterId}")
        };
        gather.Say(message);
        response.Append(gather);
        return response;
    }

    private VoiceResponse HangUp(string message)
    {
        var response = new VoiceResponse();
        if (!string.IsNullOrEmpty(message))
        {
            response.Say(message);
        }
        response.Hangup();
        return response;
    }

    private VoiceResponse HoldOn(int interval, string message = null)
    {
        var routingSetting = _services.GetRequiredService<RoutingSettings>();

        var response = new VoiceResponse();
        var gather = new Gather()
        {
            Input = new List<Gather.InputEnum>() { Gather.InputEnum.Speech },
            Action = new Uri($"{_settings.CallbackHost}/twilio/voice/{routingSetting.RouterId}"),
            ActionOnEmptyResult = true
        };
        if (!string.IsNullOrEmpty(message))
        {
            gather.Say(message);
        }
        gather.Pause(interval);
        response.Append(gather);
        return response;
    }
}
