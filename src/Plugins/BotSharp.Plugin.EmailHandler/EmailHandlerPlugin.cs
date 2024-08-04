using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Settings;
using BotSharp.Plugin.EmailHandler.Providers;
using Microsoft.Extensions.Configuration;

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
                return settingService.Bind<EmailSenderSettings>("EmailSender");
            });

            services.AddScoped<IAgentHook, EmailSenderHook>();
            services.AddScoped<IAgentHook, EmailReaderHook>();
            services.AddScoped<IAgentUtilityHook, EmailHandlerUtilityHook>();

            var emailReaderSettings = new EmailReaderSettings();
            config.Bind("EmailReader", emailReaderSettings);
            services.AddScoped(provider =>
            {
                var settingService = provider.GetRequiredService<ISettingService>();
                return settingService.Bind<EmailReaderSettings>("EmailReader");
            });
            services.AddSingleton(provider => emailReaderSettings);
            services.AddScoped<IEmailReader, DefaultEmailReader>();
        }
    }
}
