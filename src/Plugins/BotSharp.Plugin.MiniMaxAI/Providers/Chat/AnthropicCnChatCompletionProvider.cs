namespace BotSharp.Plugin.MiniMaxAI.Providers.Chat;

public class AnthropicCnChatCompletionProvider : AnthropicChatCompletionProvider
{
    public override string Provider => "minimax-anthropic-cn";

    public AnthropicCnChatCompletionProvider(
        IServiceProvider services,
        ILogger<global::BotSharp.Plugin.AnthropicAI.Providers.ChatCompletionProvider> logger,
        IConversationStateService state) : base(services, logger, state)
    {
    }
}
