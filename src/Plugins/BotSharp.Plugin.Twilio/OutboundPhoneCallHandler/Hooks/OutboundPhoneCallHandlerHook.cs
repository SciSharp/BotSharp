using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.Enums;

namespace BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.Hooks
{
    internal class OutboundPhoneCallHandlerHook : AgentHookBase
    {
        private static string OUTBOUND_PHONE_CALL_FN = "twilio_outbound_phone_call";

        public override string SelfId => string.Empty;

        public OutboundPhoneCallHandlerHook(IServiceProvider services, AgentSettings settings) : base(services, settings)
        {
        }

        public override void OnAgentLoaded(Agent agent)
        {
            var utilityLoad = new AgentUtility
            {
                Name = UtilityName.OutboundPhoneCall,
                Content = new UtilityContent
                {
                    Functions = [new(OUTBOUND_PHONE_CALL_FN)],
                    Templates = [new($"{OUTBOUND_PHONE_CALL_FN}.fn")]
                }
            };

            base.OnLoadAgentUtility(agent, [utilityLoad]);
            base.OnAgentLoaded(agent);
        }
    }
}
