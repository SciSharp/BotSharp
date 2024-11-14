using BotSharp.Plugin.Twilio.OutboundCallHandler.Hooks;

namespace BotSharp.Plugin.Twilio.OutboundCallHandler
{
    public class OutboundCallHandlerPlugin : IBotSharpPlugin
    {
        public string Id => "901ec364-01da-4602-944f-2bc49e03b8b2";
        public string Name => "Outbound Call Handler";
        public string Description => "Empower agent to make outbound call via Twilio";
        public string IconUrl => "https://cdn-icons-png.freepik.com/512/6711/6711567.png";

        public void RegisterDI(IServiceCollection services, IConfiguration config)
        {
            services.AddScoped<IAgentHook, OutboundCallHandlerHook>();
            services.AddScoped<IAgentUtilityHook, OutboundCallHandlerUtilityHook>();
        }
    }
}
