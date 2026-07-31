namespace BotSharp.Plugin.MiniMaxAI.Providers.Chat;

public class OpenAiCnChatCompletionProvider : ChatCompletionProvider
{
    public override string Provider => "minimax-cn";

    public OpenAiCnChatCompletionProvider(
        ILogger<global::BotSharp.Plugin.OpenAI.Providers.Chat.ChatCompletionProvider> logger,
        IServiceProvider services,
        IConversationStateService state,
        IFileStorageService fileStorage) : base(logger, services, state, fileStorage)
    {
    }
}
