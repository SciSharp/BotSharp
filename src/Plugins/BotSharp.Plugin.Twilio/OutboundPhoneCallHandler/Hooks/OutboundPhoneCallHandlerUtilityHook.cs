using BotSharp.Abstraction.Agents.Models;
using BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.Enums;

namespace BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.Hooks;

public class OutboundPhoneCallHandlerUtilityHook : IAgentUtilityHook
{
    private static string OUTBOUND_PHONE_CALL_FN = "twilio_outbound_phone_call";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var utility = new AgentUtility
        {
            Name = UtilityName.OutboundPhoneCall,
            Functions = [new(OUTBOUND_PHONE_CALL_FN)],
            Templates = [new($"{OUTBOUND_PHONE_CALL_FN}.fn")]
        };

        utilities.Add(utility);
    }
}
