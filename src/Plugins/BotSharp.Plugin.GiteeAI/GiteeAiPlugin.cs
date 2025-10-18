using BotSharp.Abstraction.Plugins;
using BotSharp.Plugin.GiteeAI.Providers.Chat;
using BotSharp.Plugin.GiteeAI.Providers.Embedding;

namespace BotSharp.Plugin.GiteeAI;

public  class GiteeAiPlugin : IBotSharpPlugin
{
    public string Id => "59ad4c3c-0b88-3344-ba99-5245ec015938";
    public string Name => "GiteeAI";
    public string Description => "Gitee AI";
    public string IconUrl => "https://ai-assets.gitee.com/_next/static/media/gitee-ai.622edfb0.ico";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<ITextEmbedding, TextEmbeddingProvider>();
        services.AddScoped<IChatCompletion, ChatCompletionProvider>();
    }
}
