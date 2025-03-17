using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.Routing;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.LlmContexts;
using Twilio.Rest.Api.V2010.Account;

namespace BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.Functions;

public class TransferPhoneCallFn : IFunctionCallback
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly BotSharpOptions _options;
    private readonly TwilioSetting _twilioSetting;

    public string Name => "util-twilio-transfer_phone_call";
    public string Indication => "Transferring the active line";

    public TransferPhoneCallFn(
        IServiceProvider services,
        ILogger<TransferPhoneCallFn> logger,
        BotSharpOptions options,
        TwilioSetting twilioSetting)
    {
        _services = services;
        _logger = logger;
        _options = options;
        _twilioSetting = twilioSetting;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<ForwardPhoneCallArgs>(message.FunctionArgs, _options.JsonSerializerOptions);

        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var states = _services.GetRequiredService<IConversationStateService>();
        var routing = _services.GetRequiredService<IRoutingService>();
        var conversationId = routing.Context.ConversationId;
        var processUrl = $"{_twilioSetting.CallbackHost}/twilio/voice/transfer-call?conversation-id={conversationId}&transfer-to={args.PhoneNumber}";

        // Generate initial assistant audio
        string initAudioFile = null;
        if (!string.IsNullOrEmpty(args.TransitionMessage))
        {
            var completion = CompletionProvider.GetAudioCompletion(_services, "openai", "tts-1");
            var data = await completion.GenerateAudioFromTextAsync(args.TransitionMessage);
            initAudioFile = "transfer.mp3";
            fileStorage.SaveSpeechFile(conversationId, initAudioFile, data);

            processUrl += $"&init-audio-file={initAudioFile}";
        }

        if (!string.IsNullOrEmpty(initAudioFile))
        {
            processUrl += $"&init-audio-file={initAudioFile}";
        }

        // Forward call
        var sid = states.GetState("twilio_call_sid");

        if (string.IsNullOrEmpty(sid))
        {
            _logger.LogError("Twilio call sid is empty.");
            message.Content = "There is an error when transferring the phone call.";
            return false;
        }
        else
        {
            var call = CallResource.Update(
                pathSid: sid,
                url: new Uri(processUrl));

            message.Content = args.TransitionMessage;
            return true;
        }
    }
}
