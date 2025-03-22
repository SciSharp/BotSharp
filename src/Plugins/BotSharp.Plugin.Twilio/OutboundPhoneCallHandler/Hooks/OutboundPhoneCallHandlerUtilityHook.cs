using BotSharp.Abstraction.Agents.Models;
using BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.Enums;

namespace BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.Hooks;

public class OutboundPhoneCallHandlerUtilityHook : IAgentUtilityHook
{
    private static string PREFIX = "util-twilio-";
    private static string OUTBOUND_PHONE_CALL_FN = $"{PREFIX}outbound_phone_call";
    private static string TRANSFER_PHONE_CALL_FN = $"{PREFIX}transfer_phone_call";
    private static string HANGUP_PHONE_CALL_FN = $"{PREFIX}hangup_phone_call";
    private static string TEXT_MESSAGE_FN = $"{PREFIX}text_message";
    private static string LEAVE_VOICEMAIL_FN = $"{PREFIX}leave_voicemail";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var utility = new AgentUtility
        {
            Name = UtilityName.OutboundPhoneCall,
            Functions = 
            [
                new($"{OUTBOUND_PHONE_CALL_FN}"),
                new($"{TRANSFER_PHONE_CALL_FN}"),
                new($"{HANGUP_PHONE_CALL_FN}"),
                new($"{TEXT_MESSAGE_FN}"),
                new($"{LEAVE_VOICEMAIL_FN}")
            ],
            Templates = 
            [
            ]
        };

        utilities.Add(utility);
    }
}
