using BotSharp.Plugin.Twilio.OutboundCallHandler.Enums;

namespace BotSharp.Plugin.Twilio.OutboundCallHandler.Hooks
{
    public class OutboundCallHandlerUtilityHook : IAgentUtilityHook
    {
        public void AddUtilities(List<string> utilities)
        {
            utilities.Add(UtilityName.TwilioOutboundCaller);
        }
    }
}
