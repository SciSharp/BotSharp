using BotSharp.Plugin.SparkDesk.Providers;

namespace BotSharp.Plugin.SparkDesk;

public class SparkDeskPlugin : IBotSharpPlugin
{
    public string Id => "93ad041b-a99e-5766-946e-6a8cc2d4dd4f";

    public string Name => "sparkdesk";
    public string Description => "xfyun sparkdesk Service including text generation services.";
    public SettingsMeta Settings =>  new SettingsMeta("SparkDesk");

    public object GetNewSettingsInstance()
    {
        return new SparkDeskSettings();
    }

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<SparkDeskSettings>("SparkDesk");
        });
        services.AddScoped<IChatCompletion, ChatCompletionProvider>();
    }
}
