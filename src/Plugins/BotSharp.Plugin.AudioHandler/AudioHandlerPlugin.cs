using BotSharp.Plugin.AudioHandler.Settings;
using BotSharp.Abstraction.Settings;

namespace BotSharp.Plugin.AudioHandler;

public class AudioHandlerPlugin : IBotSharpPlugin
{
    public string Id => "9d22014c-4f45-466a-9e82-a74e67983df8";
    public string Name => "Audio Handler";
    public string Description => "Process audio input and transform it into text output.";
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<AudioHandlerSettings>("AudioHandler");
        });

        services.AddScoped<IAudioCompletion, NativeWhisperProvider>();
        services.AddScoped<IAgentUtilityHook, AudioHandlerUtilityHook>();
    }
}

