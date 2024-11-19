using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Utilities;
using BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.Enums;

namespace BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.Hooks
{
    internal class OutboundPhoneCallHandlerHook : AgentHookBase
    {
        private static string FUNCTION_NAME = "twilio_outbound_phone_call";

        public override string SelfId => string.Empty;

        public OutboundPhoneCallHandlerHook(IServiceProvider services, AgentSettings settings) : base(services, settings)
        {
        }

        public override void OnAgentLoaded(Agent agent)
        {
            var conv = _services.GetRequiredService<IConversationService>();
            var isConvMode = conv.IsConversationMode();
            var isEnabled = !agent.Utilities.IsNullOrEmpty() && agent.Utilities.Contains(UtilityName.OutboundPhoneCall);

            if (isConvMode && isEnabled)
            {
                var (prompt, fn) = GetPromptAndFunction();
                if (fn != null)
                {
                    if (!string.IsNullOrWhiteSpace(prompt))
                    {
                        agent.Instruction += $"\r\n\r\n{prompt}\r\n\r\n";
                    }

                    if (agent.Functions == null)
                    {
                        agent.Functions = new List<FunctionDef> { fn };
                    }
                    else
                    {
                        agent.Functions.Add(fn);
                    }
                }
            }

            base.OnAgentLoaded(agent);
        }

        private (string, FunctionDef) GetPromptAndFunction()
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            var agent = db.GetAgent(BuiltInAgentId.UtilityAssistant);
            var prompt = agent?.Templates?.FirstOrDefault(x => x.Name.IsEqualTo($"{FUNCTION_NAME}.fn"))?.Content ?? string.Empty;
            var loadAttachmentFn = agent?.Functions?.FirstOrDefault(x => x.Name.IsEqualTo(FUNCTION_NAME));
            return (prompt, loadAttachmentFn);
        }
    }
}
