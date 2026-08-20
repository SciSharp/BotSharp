namespace BotSharp.Plugin.MiniMaxAI.Providers.Chat;

public class AnthropicChatCompletionProvider : global::BotSharp.Plugin.AnthropicAI.Providers.ChatCompletionProvider
{
    public override string Provider => "minimax-anthropic";

    public AnthropicChatCompletionProvider(
        IServiceProvider services,
        ILogger<global::BotSharp.Plugin.AnthropicAI.Providers.ChatCompletionProvider> logger,
        IConversationStateService state) : base(new(), services, logger, state)
    {
    }
}
