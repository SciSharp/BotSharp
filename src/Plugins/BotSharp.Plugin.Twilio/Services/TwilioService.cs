using BotSharp.Abstraction.Utilities;
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

    public VoiceResponse ReturnInstructions(List<string> speechPaths, string callbackPath, bool actionOnEmptyResult, int timeout = 2)
    {
        var response = new VoiceResponse();
        var gather = new Gather()
        {
            Input = new List<Gather.InputEnum>()
            {
                Gather.InputEnum.Speech,
                Gather.InputEnum.Dtmf
            },
            Action = new Uri($"{_settings.CallbackHost}/{callbackPath}"),
            SpeechModel = Gather.SpeechModelEnum.PhoneCall,
            SpeechTimeout = timeout > 0 ? timeout.ToString() : "2",
            Timeout = timeout > 0 ? timeout : 2,
            ActionOnEmptyResult = actionOnEmptyResult
        };

        if (!speechPaths.IsNullOrEmpty())
        {
            foreach (var speechPath in speechPaths)
            {
                gather.Play(new Uri($"{_settings.CallbackHost}/{speechPath}"));
            }
        }
        response.Append(gather);
        return response;
    }

    public VoiceResponse ReturnNoninterruptedInstructions(List<string> speechPaths, string callbackPath, bool actionOnEmptyResult, int timeout = 2)
    {
        var response = new VoiceResponse();
        if (speechPaths != null && speechPaths.Any())
        {
            foreach (var speechPath in speechPaths)
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
            Action = new Uri($"{_settings.CallbackHost}/{callbackPath}"),
            SpeechModel = Gather.SpeechModelEnum.PhoneCall,
            SpeechTimeout = timeout > 0 ? timeout.ToString() : "2",
            Timeout = timeout > 0 ? timeout : 2,
            ActionOnEmptyResult = actionOnEmptyResult
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
