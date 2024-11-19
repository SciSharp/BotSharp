using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Options;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.Interfaces;
using BotSharp.Plugin.Twilio.Models;
using BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.LlmContexts;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.Functions
{
    public class HandleOutboundPhoneCallFn : IFunctionCallback
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<HandleOutboundPhoneCallFn> _logger;
        private readonly BotSharpOptions _options;
        private readonly TwilioSetting _twilioSetting;

        public string Name => "twilio_outbound_phone_call";
        public string Indication => "Dialing the number";

        public HandleOutboundPhoneCallFn(
            IServiceProvider services,
            ILogger<HandleOutboundPhoneCallFn> logger,
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
                _logger.LogError("Invalid phone number format: {phone}", args.PhoneNumber);
                return false;
            }
            if (string.IsNullOrWhiteSpace(args.InitialMessage))
            {
                _logger.LogError("Initial message is empty.");
                return false;
            }
            var completion = CompletionProvider.GetAudioCompletion(_services, "openai", "tts-1");
            var fileStorage = _services.GetRequiredService<IFileStorageService>();
            var data = await completion.GenerateAudioFromTextAsync(args.InitialMessage);
            var conversationId = Guid.NewGuid().ToString();
            var fileName = $"intial.mp3";
            fileStorage.SaveSpeechFile(conversationId, fileName, data);
            // TODO: Add initial message in the new conversation
            var sessionManager = _services.GetRequiredService<ITwilioSessionManager>();
            await sessionManager.SetAssistantReplyAsync(conversationId, 0, new AssistantMessage
            {
                Content = args.InitialMessage,
                SpeechFileName = fileName
            });
            var call = await CallResource.CreateAsync(
                url: new Uri($"{_twilioSetting.CallbackHost}/twilio/voice/init-call?conversationId={conversationId}"),
                to: new PhoneNumber(args.PhoneNumber),
                from: new PhoneNumber(_twilioSetting.PhoneNumber));
            message.StopCompletion = true;
            return true;
        }
    }
}
