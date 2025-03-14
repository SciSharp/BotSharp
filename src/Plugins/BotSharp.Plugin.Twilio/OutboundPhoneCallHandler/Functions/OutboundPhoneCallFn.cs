using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Files.Models;
using BotSharp.Abstraction.Infrastructures.Enums;
using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.Routing;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.Interfaces;
using BotSharp.Plugin.Twilio.Models;
using BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.LlmContexts;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Conversation = BotSharp.Abstraction.Conversations.Models.Conversation;
using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.Functions;

public class OutboundPhoneCallFn : IFunctionCallback
{
    private readonly IServiceProvider _services;
    private readonly ILogger<OutboundPhoneCallFn> _logger;
    private readonly BotSharpOptions _options;
    private readonly TwilioSetting _twilioSetting;

    public string Name => "util-twilio-outbound_phone_call";
    public string Indication => "Dialing the phone number";

    public OutboundPhoneCallFn(
        IServiceProvider services,
        ILogger<OutboundPhoneCallFn> logger,
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
        var args = JsonSerializer.Deserialize<LlmContextIn>(message.FunctionArgs, _options.JsonSerializerOptions);
        if (args.PhoneNumber.Length != 12 || !args.PhoneNumber.StartsWith("+1", StringComparison.OrdinalIgnoreCase))
        {
            var error = $"Invalid phone number format: {args.PhoneNumber}";
            _logger.LogError(error);
            message.Content = error;
            return false;
        }

        if (string.IsNullOrWhiteSpace(args.InitialMessage))
        {
            _logger.LogError("Initial message is empty.");
            message.Content = "There is an error when generating phone message.";
            return false;
        }

        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var states = _services.GetRequiredService<IConversationStateService>();

        // Fork conversation
        var newConversationId = Guid.NewGuid().ToString();
        states.SetState(StateConst.SUB_CONVERSATION_ID, newConversationId);

        var processUrl = $"{_twilioSetting.CallbackHost}/twilio";
        var statusUrl = $"{_twilioSetting.CallbackHost}/twilio/voice/status?conversation-id={newConversationId}";
        var recordingStatusUrl = $"{_twilioSetting.CallbackHost}/twilio/recording/status?conversation-id={newConversationId}";

        // Generate initial assistant audio
        string initAudioUrl = null;
        if (!string.IsNullOrEmpty(args.InitialMessage))
        {
            var completion = CompletionProvider.GetAudioCompletion(_services, "openai", "tts-1");
            var data = await completion.GenerateAudioFromTextAsync(args.InitialMessage);
            initAudioUrl = "intial.mp3";
            fileStorage.SaveSpeechFile(newConversationId, initAudioUrl, data);

            statusUrl += $"&init-audio-file={initAudioUrl}";
        }

        // Set up process URL streaming or synchronous
        if (_twilioSetting.StreamingEnabled)
        {
            processUrl += "/stream";
        }
        else
        {
            var sessionManager = _services.GetRequiredService<ITwilioSessionManager>();
            await sessionManager.SetAssistantReplyAsync(newConversationId, 0, new AssistantMessage
            {
                Content = args.InitialMessage,
                SpeechFileName = initAudioUrl
            });

            processUrl += "/voice/init-outbound-call";
        }

        processUrl += $"?conversation-id={newConversationId}&init-audio-file={initAudioUrl}";

        // Make outbound call
        var call = await CallResource.CreateAsync(
            url: new Uri(processUrl),
            to: new PhoneNumber(args.PhoneNumber),
            from: new PhoneNumber(_twilioSetting.PhoneNumber),
            statusCallback: new Uri(statusUrl),
            // https://www.twilio.com/docs/voice/answering-machine-detection
            machineDetection: _twilioSetting.MachineDetection,
            record: _twilioSetting.RecordingEnabled,
            recordingStatusCallback: $"{_twilioSetting.CallbackHost}/twilio/record/status?conversation-id={newConversationId}");

        var convService = _services.GetRequiredService<IConversationService>();
        var routing = _services.GetRequiredService<IRoutingContext>();
        var originConversationId = convService.ConversationId;
        var entryAgentId = routing.EntryAgentId;
        
        await ForkConversation(args, entryAgentId, originConversationId, newConversationId, call);

        message.Content = $"The generated phone initial message: \"{args.InitialMessage}.\" [NEW CONVERSATION ID: {newConversationId}, TWILIO CALL SID: {call.Sid}, STREAMING: {_twilioSetting.StreamingEnabled}, RECORDING: {_twilioSetting.RecordingEnabled}]";
        message.StopCompletion = true;
        return true;
    }

    private async Task ForkConversation(LlmContextIn args, 
        string entryAgentId, 
        string originConversationId, 
        string newConversationId,
        CallResource resource)
    {
        // new scope service for isolated conversation
        using var scope = _services.CreateScope();
        var services = scope.ServiceProvider;
        var convService = services.GetRequiredService<IConversationService>();
        var convStorage = services.GetRequiredService<IConversationStorage>();

        var newConv = await convService.NewConversation(new Conversation
        {
            Id = newConversationId,
            AgentId = entryAgentId,
            Channel = ConversationChannel.Phone,
            ChannelId = resource.Sid,
            Title = args.InitialMessage
        });

        convStorage.Append(newConversationId, new List<RoleDialogModel>
        {
            new RoleDialogModel(AgentRole.User, "Hi")
            {
                CurrentAgentId = entryAgentId
            },
            new RoleDialogModel(AgentRole.Assistant, args.InitialMessage)
            {
                CurrentAgentId = entryAgentId
            }
        });

        convService.SetConversationId(newConversationId, 
        [
            new MessageState(StateConst.ORIGIN_CONVERSATION_ID, originConversationId),
            new MessageState("phone_number", resource.To)
        ]);
        convService.SaveStates();
    }
}
