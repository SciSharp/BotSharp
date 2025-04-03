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
        var statusUrl = $"{_twilioSetting.CallbackHost}/twilio/voice/status?agent-id={message.CurrentAgentId}&conversation-id={newConversationId}";
        var recordingStatusUrl = $"{_twilioSetting.CallbackHost}/twilio/recording/status?agent-id={message.CurrentAgentId}&conversation-id={newConversationId}";

        // Generate initial assistant audio
        string initAudioFile = null;
        if (!string.IsNullOrEmpty(args.InitialMessage))
        {
            var completion = CompletionProvider.GetAudioSynthesizer(_services);
            var data = await completion.GenerateAudioAsync(args.InitialMessage);
            initAudioFile = "intial.mp3";
            fileStorage.SaveSpeechFile(newConversationId, initAudioFile, data);

            statusUrl += $"&init-audio-file={initAudioFile}";
        }

        // load agent profile
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.GetAgent(message.CurrentAgentId);

        // Set up process URL streaming or synchronous
        if (agent.Profiles.Contains("realtime"))
        {
            processUrl += "/inbound";
        }
        else
        {
            var sessionManager = _services.GetRequiredService<ITwilioSessionManager>();
            await sessionManager.SetAssistantReplyAsync(newConversationId, 0, new AssistantMessage
            {
                Content = args.InitialMessage,
                SpeechFileName = initAudioFile
            });

            processUrl += "/voice/init-outbound-call";
        }

        processUrl += $"?agent-id={message.CurrentAgentId}&conversation-id={newConversationId}";
        if (!string.IsNullOrEmpty(initAudioFile))
        {
            processUrl += $"&init-audio-file={initAudioFile}";
        }

        // Make outbound call
        var call = await CallResource.CreateAsync(
            url: new Uri(processUrl),
            to: new PhoneNumber(args.PhoneNumber),
            from: new PhoneNumber(_twilioSetting.PhoneNumber),
            statusCallback: new Uri(statusUrl),
            // https://www.twilio.com/docs/voice/answering-machine-detection
            machineDetection: _twilioSetting.MachineDetection,
            machineDetectionSilenceTimeout: _twilioSetting.MachineDetectionSilenceTimeout,
            record: _twilioSetting.RecordingEnabled,
            recordingStatusCallback: $"{_twilioSetting.CallbackHost}/twilio/record/status?agent-id={message.CurrentAgentId}&conversation-id={newConversationId}");

        var convService = _services.GetRequiredService<IConversationService>();
        var routing = _services.GetRequiredService<IRoutingContext>();
        var originConversationId = convService.ConversationId;
        var entryAgentId = routing.EntryAgentId;
        
        await ForkConversation(args, entryAgentId, originConversationId, newConversationId, call);

        message.Content = $"The generated phone initial message: \"{args.InitialMessage}.\" [NEW CONVERSATION ID: {newConversationId}, TWILIO CALL SID: {call.Sid}, RECORDING: {_twilioSetting.RecordingEnabled}]";
        message.StopCompletion = true;
        return true;
    }

    private async Task ForkConversation(LlmContextIn args, 
        string entryAgentId, 
        string originConversationId, 
        string newConversationId,
        CallResource call)
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
            ChannelId = call.Sid,
            Title = args.InitialMessage
        });

        var messageId = Guid.NewGuid().ToString();
        convStorage.Append(newConversationId, new List<RoleDialogModel>
        {
            new RoleDialogModel(AgentRole.User, "Hi")
            {
                MessageId = messageId,
                CurrentAgentId = entryAgentId
            },
            new RoleDialogModel(AgentRole.Assistant, args.InitialMessage)
            {
                MessageId = messageId,
                CurrentAgentId = entryAgentId
            }
        });

        convService.SetConversationId(newConversationId, 
        [
            new MessageState(StateConst.ORIGIN_CONVERSATION_ID, originConversationId),
            new MessageState("channel", "phone"),
            new MessageState("phone_from", call.From),
            new MessageState("phone_direction", call.Direction),
            new MessageState("phone_number", call.To),
            new MessageState("twilio_call_sid", call.Sid)
        ]);
        convService.SaveStates();
    }
}
