using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Routing;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.LlmContexts;
using BotSharp.Plugin.Twilio.Services;

namespace BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.Functions;

public class LeaveVoicemailFn : IFunctionCallback
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly TwilioSetting _setting;

    public string Name => "util-twilio-leave_voicemail";
    public string Indication => "leaving a voicemail";

    public LeaveVoicemailFn(
        IServiceProvider services,
        ILogger<LeaveVoicemailFn> logger,
        TwilioSetting setting)
    {
        _services = services;
        _logger = logger;
        _setting = setting;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<LeaveVoicemailArgs>(message.FunctionArgs);

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

        // Generate voice message audio
        if (!string.IsNullOrEmpty(args.VoicemailMessage))
        {
            var twilio = _services.GetRequiredService<TwilioService>();
            await twilio.SaveSpeechAsync(conversationId, "voicemail", args.VoicemailMessage);
        }

        message.Content = args.VoicemailMessage;
        message.StopCompletion = true;

        return true;
    }
}
