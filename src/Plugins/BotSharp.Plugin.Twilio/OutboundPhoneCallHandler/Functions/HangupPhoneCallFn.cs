using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Routing;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.LlmContexts;
using Twilio.Rest.Api.V2010.Account;

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

        var fileStorage = _services.GetRequiredService<IFileStorageService>();
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

        // Generate initial assistant audio
        string initAudioFile = null;
        if (!string.IsNullOrEmpty(args.ResponseContent))
        {
            var completion = CompletionProvider.GetAudioSynthesizer(_services);
            var data = await completion.GenerateAudioAsync(args.ResponseContent);
            initAudioFile = "ending.mp3";
            fileStorage.SaveSpeechFile(conversationId, initAudioFile, data);

            processUrl += $"&init-audio-file={initAudioFile}";
        }

        var call = CallResource.Update(
            url: new Uri(processUrl),
            pathSid: callSid
        );

        message.Content = args.Reason;
        message.StopCompletion = true;

        return true;
    }
}
