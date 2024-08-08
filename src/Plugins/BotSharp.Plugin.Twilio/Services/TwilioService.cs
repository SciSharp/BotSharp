using BotSharp.Plugin.Twilio.Settings;
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
                Gather.InputEnum.Speech
            },
            Action = new Uri($"{_settings.CallbackHost}/twilio/voice/{twilioSetting.AgentId}")
        };
        gather.Say(message);
        response.Append(gather);
        return response;
    }

    public VoiceResponse ReturnInstructions(string speechPath, string callbackPath, bool actionOnEmptyResult)
    {
        var response = new VoiceResponse();
        var gather = new Gather()
        {
            Input = new List<Gather.InputEnum>()
            {
                Gather.InputEnum.Speech
            },
            Action = new Uri($"{_settings.CallbackHost}/{callbackPath}"),
            ActionOnEmptyResult = actionOnEmptyResult
        };
        if (!string.IsNullOrEmpty(speechPath))
        {
            gather.Play(new Uri($"{_settings.CallbackHost}/{speechPath}"));
        }
        response.Append(gather);
        return response;
    }

    public VoiceResponse DummyInstructions(string message, string callbackPath, bool actionOnEmptyResult)
    {
        var response = new VoiceResponse();
        var gather = new Gather()
        {
            Input = new List<Gather.InputEnum>()
            {
                Gather.InputEnum.Speech
            },
            Action = new Uri($"{_settings.CallbackHost}/{callbackPath}"),
            ActionOnEmptyResult = actionOnEmptyResult
        };
        if (!string.IsNullOrEmpty(message))
        {
            gather.Say(message);
        }
        response.Append(gather);
        return response;
    }

    public VoiceResponse HangUp(string message)
    {
        var response = new VoiceResponse();
        if (!string.IsNullOrEmpty(message))
        {
            response.Say(message);
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
            Input = new List<Gather.InputEnum>() { Gather.InputEnum.Speech },
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
