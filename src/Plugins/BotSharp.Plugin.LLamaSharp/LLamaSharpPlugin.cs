using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Plugins;
using BotSharp.Plugin.LLamaSharp.Providers;
using BotSharp.Plugin.LLamaSharp.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Plugins.LLamaSharp;

public class LLamaSharpPlugin : IBotSharpPlugin
{
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
