using BotSharp.Plugin.FileHandler.Hooks;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.FileHandler;

public class FileHandlerPlugin : IBotSharpPlugin
{
    public string Id => "65be5aee-48df-4ff8-a50a-05c8bcd2a793";
    public string Name => "File Handler";
    public string Description => "AI handles files.";
    public string IconUrl => "https://cdn-icons-png.flaticon.com/512/2567/2567656.png";
    public string[] AgentIds => [];

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<FileHandlerSettings>("FileHandler");
        });

        services.AddScoped<IAgentUtilityHook, FileHandlerUtilityHook>();
    }
}