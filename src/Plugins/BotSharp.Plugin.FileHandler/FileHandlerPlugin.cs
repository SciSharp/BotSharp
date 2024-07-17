using BotSharp.Plugin.FileHandler.Hooks;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.FileHandler;

public class FileHandlerPlugin : IBotSharpPlugin
{
    public string Id => "65be5aee-48df-4ff8-a50a-05c8bcd2a793";
    public string Name => "File Handler";
    public string Description => "AI reads files, such as image, pdf, excel";
    public string IconUrl => "https://lirp.cdn-website.com/6f8d6d8a/dms3rep/multi/opt/API_Icon-640w.png";
    public string[] AgentIds => [];

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<FileHandlerSettings>("FileHandler");
        });

        services.AddScoped<IAgentHook, FileHandlerHook>();
        services.AddScoped<IAgentUtilityHook, FileHandlerUtilityHook>();
    }

}