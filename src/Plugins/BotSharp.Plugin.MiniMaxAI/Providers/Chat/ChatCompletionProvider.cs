namespace BotSharp.Plugin.MiniMaxAI.Providers.Chat;

public class ChatCompletionProvider : global::BotSharp.Plugin.OpenAI.Providers.Chat.ChatCompletionProvider
{
    public override string Provider => "minimax";

    public ChatCompletionProvider(
        global::BotSharp.Plugin.OpenAI.Settings.OpenAiSettings settings,
        ILogger<global::BotSharp.Plugin.OpenAI.Providers.Chat.ChatCompletionProvider> logger,
        IServiceProvider services,
        IConversationStateService state,
        IFileStorageService fileStorage) : base(settings, logger, services, state, fileStorage)
    {
    }
}
