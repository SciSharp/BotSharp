using BotSharp.Abstraction.Options;
using BotSharp.Plugin.Twilio.OutboundCallHandler.LlmContexts;

namespace BotSharp.Plugin.Twilio.OutboundCallHandler.Functions
{
    public class HandleOutboundCallFn : IFunctionCallback
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<HandleOutboundCallFn> _logger;
        private readonly BotSharpOptions _options;
        private readonly TwilioSetting _twilioSetting;

        public string Name => "handle_outbound_call";
        public string Indication => "Dialing the number";

        public HandleOutboundCallFn(
            IServiceProvider services,
            ILogger<HandleOutboundCallFn> logger,
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
            return true;
        }
    }
}
