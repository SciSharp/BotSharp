using BotSharp.Plugin.ImageHandler.Converters;
using BotSharp.Plugin.ImageHandler.Hooks;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.ImageHandler;

public class ImageHandlerPlugin : IBotSharpPlugin
{
    public string Id => "bac8bbf3-da91-4c92-98d8-db14d68e75ae";
    public string Name => "Image Handler";
    public string Description => "AI handles images.";
    public string IconUrl => "https://cdn-icons-png.flaticon.com/512/8002/8002135.png";
    public string[] AgentIds => [];

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<ImageHandlerSettings>("ImageHandler");
        });

        services.AddScoped<IAgentUtilityHook, ImageHandlerUtilityHook>();
        services.AddScoped<IImageConverter, ImageHandlerImageConverter>();
    }
}