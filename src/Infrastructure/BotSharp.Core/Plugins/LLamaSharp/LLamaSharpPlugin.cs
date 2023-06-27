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
        services.AddScoped<ITextEmbedding, TextEmbeddingProvider>();
        services.AddScoped<IChatCompletion, ChatCompletionProvider>();
    }
}
