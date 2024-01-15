using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.Settings;
using BotSharp.Plugin.LLamaSharp.Providers;

namespace BotSharp.Plugins.LLamaSharp;

public class LLamaSharpPlugin : IBotSharpPlugin
{
    public string Id => "3999f668-9fbf-4a91-bd4d-df5b7dfcd90e";
    public string Name => "LLamaSharp";
    public SettingsMeta Settings => 
        new SettingsMeta("LLamaSharp");
    public object GetNewSettingsInstance()
    {
        return new LlamaSharpSettings();
    }

    public string Description => "The C#/.NET binding of llama.cpp. Run local LLaMA/GPT model easily and fast in C#!";
    public string IconUrl => "https://raw.githubusercontent.com/SciSharp/LLamaSharp/master/Assets/LLamaSharpLogo.png";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton(provider =>
        {
            var settingService = provider.CreateScope().ServiceProvider.GetRequiredService<ISettingService>();
            return settingService.Bind<LlamaSharpSettings>("LlamaSharp");
        });

        services.AddSingleton<LlamaAiModel>();
        services.AddSingleton<ITextEmbedding, TextEmbeddingProvider>();
        services.AddScoped<ITextCompletion, TextCompletionProvider>();
        services.AddScoped<IChatCompletion, ChatCompletionProvider>();
    }
}
