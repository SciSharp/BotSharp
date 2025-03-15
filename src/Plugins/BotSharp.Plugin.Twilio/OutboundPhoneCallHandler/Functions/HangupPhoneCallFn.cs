using BotSharp.Abstraction.Routing;
using BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.LlmContexts;
using Twilio.Rest.Api.V2010.Account;

namespace BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.Functions;

public class HangupPhoneCallFn : IFunctionCallback
{
    private readonly IServiceProvider _services;
    private readonly ILogger<HangupPhoneCallFn> _logger;
    private readonly TwilioSetting _twilioSetting;

    public string Name => "util-twilio-hangup_phone_call";
    public string Indication => "Hangup";

    public HangupPhoneCallFn(
        IServiceProvider services,
        ILogger<HangupPhoneCallFn> logger,
        TwilioSetting twilioSetting)
    {
        _services = services;
        _logger = logger;
        _twilioSetting = twilioSetting;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<HangupPhoneCallArgs>(message.FunctionArgs);

        var routing = _services.GetRequiredService<IRoutingService>();
        var conversationId = routing.Context.ConversationId;
        var states = _services.GetRequiredService<IConversationStateService>();
        var callSid = states.GetState("twilio_call_sid");

        if (string.IsNullOrEmpty(callSid))
        {
            message.Content = "The call has not been initiated.";
            _logger.LogError(message.Content);
            return false;
        }

        if (args.AnythingElseToHelp)
        {
            message.Content = "Tell me how I can help.";
        }
        else
        {
            var call = CallResource.Update(
                url: new Uri($"{_twilioSetting.CallbackHost}/twilio/voice/hang-up?conversation-id={conversationId}"),
                pathSid: callSid
            );

            message.Content = "The call is ending.";
            message.StopCompletion = true;
        }

        return true;
    }
}
