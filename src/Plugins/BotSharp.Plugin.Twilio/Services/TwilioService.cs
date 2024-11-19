using BotSharp.Abstraction.Utilities;
using BotSharp.Plugin.Twilio.Models;
using Twilio.Jwt.AccessToken;
using Token = Twilio.Jwt.AccessToken.Token;

namespace BotSharp.Plugin.Twilio.Services;

/// <summary>
/// https://github.com/TwilioDevEd/voice-javascript-sdk-quickstart-csharp
/// </summary>
public class TwilioService
{
    private readonly TwilioSetting _settings;
    private readonly IServiceProvider _services;

    public TwilioService(TwilioSetting settings, IServiceProvider services)
    {
        _settings = settings;
        _services = services;
    }

    public string GetAccessToken()
    {
        // These are specific to Voice
        var user = _services.GetRequiredService<IUserIdentity>();

        // Create a Voice grant for this token
        var grant = new VoiceGrant();
        grant.OutgoingApplicationSid = _settings.AppSID;

        // Optional: add to allow incoming calls
        grant.IncomingAllow = true;

        var grants = new HashSet<IGrant>
        {
            { grant }
        };

        // Create an Access Token generator
        var token = new Token(
            _settings.AccountSID,
            _settings.ApiKeySID,
            _settings.ApiSecret,
            user.Id,
            grants: grants);

        return token.ToJwt();
    }

    public VoiceResponse ReturnInstructions(string message)
    {
        var twilioSetting = _services.GetRequiredService<TwilioSetting>();

        var response = new VoiceResponse();
        var gather = new Gather()
        {
            Input = new List<Gather.InputEnum>()
            {
                Gather.InputEnum.Speech,
                Gather.InputEnum.Dtmf
            },
            Action = new Uri($"{_settings.CallbackHost}/twilio/voice/{twilioSetting.AgentId}")
        };

        gather.Say(message);
        response.Append(gather);
        return response;
    }

    public VoiceResponse ReturnInstructions(ConversationalVoiceResponse conversationalVoiceResponse)
    {
        var response = new VoiceResponse();
        var gather = new Gather()
        {
            Input = new List<Gather.InputEnum>()
            {
                Gather.InputEnum.Speech,
                Gather.InputEnum.Dtmf
            },
            Action = new Uri($"{_settings.CallbackHost}/{conversationalVoiceResponse.CallbackPath}"),
            SpeechModel = Gather.SpeechModelEnum.PhoneCall,
            SpeechTimeout = "auto", // timeout > 0 ? timeout.ToString() : "3",
            Timeout = conversationalVoiceResponse.Timeout > 0 ? conversationalVoiceResponse.Timeout : 3,
            ActionOnEmptyResult = conversationalVoiceResponse.ActionOnEmptyResult,
            Hints = conversationalVoiceResponse.Hints
        };

        if (!conversationalVoiceResponse.SpeechPaths.IsNullOrEmpty())
        {
            foreach (var speechPath in conversationalVoiceResponse.SpeechPaths)
            {
                gather.Play(new Uri($"{_settings.CallbackHost}/{speechPath}"));
            }
        }
        response.Append(gather);
        return response;
    }

    public VoiceResponse ReturnNoninterruptedInstructions(ConversationalVoiceResponse conversationalVoiceResponse)
    {
        var response = new VoiceResponse();
        response.Pause(2);
        if (conversationalVoiceResponse.SpeechPaths != null && conversationalVoiceResponse.SpeechPaths.Any())
        {
            foreach (var speechPath in conversationalVoiceResponse.SpeechPaths)
            {
                response.Play(new Uri($"{_settings.CallbackHost}/{speechPath}"));
            }
        }
        var gather = new Gather()
        {
            Input = new List<Gather.InputEnum>()
            {
                Gather.InputEnum.Speech,
                Gather.InputEnum.Dtmf
            },
            Action = new Uri($"{_settings.CallbackHost}/{conversationalVoiceResponse.CallbackPath}"),
            SpeechModel = Gather.SpeechModelEnum.PhoneCall,
            SpeechTimeout = "auto", // conversationalVoiceResponse.Timeout > 0 ? conversationalVoiceResponse.Timeout.ToString() : "3",
            Timeout = conversationalVoiceResponse.Timeout > 0 ? conversationalVoiceResponse.Timeout : 3,
            ActionOnEmptyResult = conversationalVoiceResponse.ActionOnEmptyResult
        };
        response.Append(gather);
        return response;
    }

    public VoiceResponse HangUp(string speechPath)
    {
        var response = new VoiceResponse();
        if (!string.IsNullOrEmpty(speechPath))
        {
            response.Play(new Uri($"{_settings.CallbackHost}/{speechPath}"));
        }
        response.Hangup();
        return response;
    }

    public VoiceResponse DialCsrAgent(string speechPath)
    {
        var response = new VoiceResponse();
        if (!string.IsNullOrEmpty(speechPath))
        {
            response.Play(new Uri($"{_settings.CallbackHost}/{speechPath}"));
        }
        response.Dial(_settings.CsrAgentNumber);
        return response;
    }

    public VoiceResponse HoldOn(int interval, string message = null)
    {
        var twilioSetting = _services.GetRequiredService<TwilioSetting>();

        var response = new VoiceResponse();
        var gather = new Gather()
        {
            Input = new List<Gather.InputEnum>()
            {
                Gather.InputEnum.Speech,
                Gather.InputEnum.Dtmf
            },
            Action = new Uri($"{_settings.CallbackHost}/twilio/voice/{twilioSetting.AgentId}"),
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
