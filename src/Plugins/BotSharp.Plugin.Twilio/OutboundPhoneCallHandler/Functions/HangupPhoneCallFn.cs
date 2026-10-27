using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Routing;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.LlmContexts;
using BotSharp.Plugin.Twilio.Services;

namespace BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.Functions;

public class HangupPhoneCallFn : IFunctionCallback
{
    private readonly IServiceProvider _services;
    private readonly ILogger<HangupPhoneCallFn> _logger;
    private readonly TwilioSetting _twilioSetting;

    public string Name => "util-twilio-hangup_phone_call";

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

        var convService = _services.GetRequiredService<IConversationService>();
        var conversationId = convService.ConversationId;
        var states = _services.GetRequiredService<IConversationStateService>();
        var callSid = states.GetState("twilio_call_sid");

        if (string.IsNullOrEmpty(callSid))
        {
            message.Content = "Please hang up the phone directly.";
            _logger.LogError(message.Content);
            return false;
        }

        var processUrl = $"{_twilioSetting.CallbackHost}/twilio/voice/hang-up?agent-id={message.CurrentAgentId}&conversation-id={conversationId}";
        var twilio = _services.GetRequiredService<TwilioService>();

        // Generate initial assistant audio
        if (!string.IsNullOrEmpty(args.ResponseContent) && _twilioSetting.GenerateEndingAudio)
        {
            var initAudioFile = await twilio.SaveSpeechAsync(conversationId, "ending", args.ResponseContent);

            processUrl += $"&init-audio-file={initAudioFile}";
        }

        // A call that already ended is what hanging up wants, so it still counts as hung up
        await twilio.TryRedirectCallAsync(callSid, processUrl);

        message.Content = args.Reason;
        message.StopCompletion = true;

        return true;
    }
}
