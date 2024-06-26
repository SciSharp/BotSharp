namespace BotSharp.Plugin.AzureOpenAI.Providers;

public class OpenAiChatCompletionProvider : ChatCompletionProvider
{
    public override string Provider => "openai";

    public OpenAiChatCompletionProvider(AzureOpenAiSettings settings,
        ILogger<OpenAiChatCompletionProvider> logger,
        IServiceProvider services) : base(settings, logger, services)
    {
    }
}
