using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Repositories.Enums;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Settings;
using BotSharp.Core.Repository;
using BotSharp.Plugin.EmailReader.Hooks;
using BotSharp.Plugin.EmailReader.Settings;
using EntityFrameworkCore.BootKit;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.EmailReader.Providers;

namespace BotSharp.Plugin.EmailReader;

public class EmailReaderPlugin : IBotSharpPlugin
{
    public string Id => "c88d27c8-127e-4aff-9cf4-74b49eec2926";
    public string Name => "Email Reader";
    public string Description => "Empower agent to read messages from email";
    public string IconUrl => "https://cdn-icons-png.freepik.com/512/6711/6711567.png";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var emailReaderSettings = new EmailReaderSettings();
        config.Bind("EmailReader", emailReaderSettings);
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<EmailReaderSettings>("EmailReader");
        });
        services.AddSingleton(provider => emailReaderSettings);
        services.AddScoped<IAgentHook, EmailReaderHook>();
        services.AddScoped<IAgentUtilityHook, EmailReaderUtilityHook>();
        services.AddScoped<IEmailReader, DefaultEmailReader>();
    }
}
