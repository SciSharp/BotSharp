using BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.Enums;

namespace BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.Hooks
{
    public class OutboundPhoneCallHandlerUtilityHook : IAgentUtilityHook
    {
        public void AddUtilities(List<string> utilities)
        {
            utilities.Add(UtilityName.OutboundPhoneCall);
        }
    }
}
