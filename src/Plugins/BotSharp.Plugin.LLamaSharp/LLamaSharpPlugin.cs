using BotSharp.Abstraction.Plugins;
using BotSharp.Plugin.LLamaSharp.Providers;

namespace BotSharp.Plugins.LLamaSharp;

public class LLamaSharpPlugin : IBotSharpPlugin
{
    public string Name => "LLamaSharp";
    public string Description => "The C#/.NET binding of llama.cpp. Run local LLaMA/GPT model easily and fast in C#!";
    public string IconUrl => "https://raw.githubusercontent.com/SciSharp/LLamaSharp/master/Assets/LLamaSharpLogo.png";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var llamaSharpSettings = new LlamaSharpSettings();
        config.Bind("LlamaSharp", llamaSharpSettings);
        services.AddSingleton(x => llamaSharpSettings);

        services.AddSingleton<LlamaAiModel>();
        services.AddSingleton<ITextEmbedding, TextEmbeddingProvider>();
        services.AddScoped<ITextCompletion, TextCompletionProvider>();
        services.AddScoped<IChatCompletion, ChatCompletionProvider>();
    }
}
