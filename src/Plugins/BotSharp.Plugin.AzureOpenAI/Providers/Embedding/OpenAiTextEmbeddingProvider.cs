namespace BotSharp.Plugin.AzureOpenAI.Providers.Embedding;

public class OpenAiTextEmbeddingProvider : TextEmbeddingProvider
{
    public override string Provider => "openai";

    public OpenAiTextEmbeddingProvider(AzureOpenAiSettings settings,
        ILogger<OpenAiTextEmbeddingProvider> logger,
        IServiceProvider services) : base(settings, logger, services) { }
}
