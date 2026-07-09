namespace BotSharp.Plugin.MiniMaxAI.Providers.Text;

public class TextCompletionProvider : global::BotSharp.Plugin.OpenAI.Providers.Text.TextCompletionProvider
{
    public override string Provider => "minimax";

    public TextCompletionProvider(
        global::BotSharp.Plugin.OpenAI.Settings.OpenAiSettings settings,
        ILogger<global::BotSharp.Plugin.OpenAI.Providers.Text.TextCompletionProvider> logger,
        IServiceProvider services) : base(settings, logger, services)
    {
    }
}
