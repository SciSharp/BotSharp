using BotSharp.Abstraction.Options;
using BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.LlmContexts;
using Twilio.Rest.Api.V2010.Account;

namespace BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.Functions;

public class TextMessageFn : IFunctionCallback
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly TwilioSetting _twilioSetting;

    public string Name => "util-twilio-text_message";
    public string Indication => "Sending text message";

    public TextMessageFn(
        IServiceProvider services,
        ILogger<TextMessageFn> logger,
        TwilioSetting twilioSetting)
    {
        _services = services;
        _logger = logger;
        _twilioSetting = twilioSetting;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<LlmContextIn>(message.FunctionArgs, BotSharpOptions.defaultJsonOptions);

        // Send the message
        var twilioMessage = MessageResource.Create(
            to: args.PhoneNumber,
            from: _twilioSetting.MessagingShortCode,
            body: args.InitialMessage
        );

        if (twilioMessage.Status == MessageResource.StatusEnum.Queued)
        {
            message.Content = $"Queued message to {args.PhoneNumber}: {args.InitialMessage} [MESSAGING SID: {twilioMessage.Sid}]";
            message.StopCompletion = true;
        }
        else
        {
            message.Content = twilioMessage.ErrorMessage;
        }

        return true;
    }
}
