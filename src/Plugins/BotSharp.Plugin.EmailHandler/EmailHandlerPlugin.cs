using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Email.Settings;
using BotSharp.Abstraction.Settings;
using BotSharp.Plugin.EmailHandler.Hooks;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Plugin.EmailHandler
{
    public class EmailHandlerPlugin : IBotSharpPlugin
    {
        public string Id => "a8e217de-e413-47a8-bbf1-af9207392a63";
        public string Name => "Email Handler";
        public string Description => "Empower agent to handle sending out emails";
        public string IconUrl => "https://cdn-icons-png.freepik.com/512/6711/6711567.png";

        public void RegisterDI(IServiceCollection services, IConfiguration config)
        {
            services.AddScoped(provider =>
            {
                var settingService = provider.GetRequiredService<ISettingService>();
                return settingService.Bind<EmailPluginSettings>("EmailPlugin");
            });

            services.AddScoped<IAgentHook, EmailHandlerHook>();
            services.AddScoped<IAgentUtilityHook, EmailHandlerUtilityHook>();
        }
    }
}
