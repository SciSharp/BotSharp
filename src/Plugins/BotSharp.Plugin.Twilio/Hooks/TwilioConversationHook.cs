using BotSharp.Abstraction.Routing;
using Task = System.Threading.Tasks.Task;
using Twilio.Rest.Api.V2010.Account;
using BotSharp.Plugin.Twilio.Interfaces;

namespace BotSharp.Plugin.Twilio.Hooks;

public class TwilioConversationHook : ConversationHookBase, IConversationHook
{
    private readonly IServiceProvider _services;
    private readonly TwilioSetting _setting;
    private readonly ILogger _logger;

    public TwilioConversationHook(IServiceProvider services,
        TwilioSetting setting,
        ILogger<TwilioConversationHook> logger)
    {
        _services = services;
        _setting = setting;
        _logger = logger;
    }

    public override async Task OnFunctionExecuted(RoleDialogModel message)
    {
        var hooks = _services.GetServices<ITwilioSessionHook>();
        foreach (var hook in hooks)
        {
            if (await hook.ShouldReconnect(message))
            {
                var states = _services.GetRequiredService<IConversationStateService>();
                var sid = states.GetState("twilio_call_sid");

                var routing = _services.GetRequiredService<IRoutingService>();
                var conversationId = routing.Context.ConversationId;
                var processUrl = $"{_setting.CallbackHost}/twilio/stream/reconnect?agent-id={message.CurrentAgentId}&conversation-id={conversationId}";

                // Save all states before reconnect
                states.Save();

                CallResource.Update(
                    pathSid: sid,
                    url: new Uri(processUrl));

                break;
            }
        }
    }
}
