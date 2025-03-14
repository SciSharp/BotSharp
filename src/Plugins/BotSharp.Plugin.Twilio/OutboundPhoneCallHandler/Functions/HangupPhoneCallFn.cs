using BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.LlmContexts;
using Twilio.Rest.Api.V2010.Account;
using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.Functions;

public class HangupPhoneCallFn : IFunctionCallback
{
    private readonly IServiceProvider _services;
    private readonly ILogger<HangupPhoneCallFn> _logger;

    public string Name => "util-twilio-hangup_phone_call";
    public string Indication => "Hangup";

    public HangupPhoneCallFn(
        IServiceProvider services,
        ILogger<HangupPhoneCallFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<HangupPhoneCallArgs>(message.FunctionArgs);
        var states = _services.GetRequiredService<IConversationStateService>();
        var callSid = states.GetState("twilio_call_sid");

        if (string.IsNullOrEmpty(callSid))
        {
            message.Content = "The call has not been initiated.";
            _logger.LogError(message.Content);
            return false;
        }

        message.Content = args.GoodbyeMessage;

        _ = Task.Run(async () =>
        {
            await Task.Delay(args.GoodbyeMessage.Split(' ').Length * 400);
            // Have to find the SID by the phone number
            var call = CallResource.Update(
                status: CallResource.UpdateStatusEnum.Completed,
                pathSid: callSid
            );

            message.Content = "The call has been ended.";
            message.StopCompletion = true;
        });

        return true;
    }
}
