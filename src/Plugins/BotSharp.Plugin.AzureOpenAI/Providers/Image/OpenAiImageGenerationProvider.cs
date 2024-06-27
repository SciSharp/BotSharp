namespace BotSharp.Plugin.AzureOpenAI.Providers.Image;

public class OpenAiImageGenerationProvider : ImageGenerationProvider
{
    public override string Provider => "openai";

    public OpenAiImageGenerationProvider(AzureOpenAiSettings settings,
        ILogger<OpenAiImageGenerationProvider> logger,
        IServiceProvider services) : base(settings, logger, services)
    {
    }
}
