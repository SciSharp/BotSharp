using BotSharp.Abstraction.MLTasks;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.Plugins.LLamaSharp;

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
