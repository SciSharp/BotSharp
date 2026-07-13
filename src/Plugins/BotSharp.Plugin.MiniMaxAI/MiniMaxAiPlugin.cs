using AnthropicChatCompletionProvider = BotSharp.Plugin.MiniMaxAI.Providers.Chat.AnthropicChatCompletionProvider;
using AnthropicCnChatCompletionProvider = BotSharp.Plugin.MiniMaxAI.Providers.Chat.AnthropicCnChatCompletionProvider;
using OpenAiChatCompletionProvider = BotSharp.Plugin.MiniMaxAI.Providers.Chat.ChatCompletionProvider;
using OpenAiCnChatCompletionProvider = BotSharp.Plugin.MiniMaxAI.Providers.Chat.OpenAiCnChatCompletionProvider;

namespace BotSharp.Plugin.MiniMaxAI;

[PluginDependency("BotSharp.Plugin.OpenAI", "BotSharp.Plugin.AnthropicAI")]
public class MiniMaxAiPlugin : IBotSharpPlugin
{
    public string Id => "8a4ebd68-4d7d-4c5c-aed9-263946cc3a0d";
    public string Name => "MiniMax";
    public string Description => "MiniMax AI models exposed through the OpenAI-compatible API.";
    public string IconUrl => "https://www.minimax.io/favicon.ico";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IChatCompletion, OpenAiChatCompletionProvider>();
        services.AddScoped<IChatCompletion, OpenAiCnChatCompletionProvider>();
        services.AddScoped<IChatCompletion, AnthropicChatCompletionProvider>();
        services.AddScoped<IChatCompletion, AnthropicCnChatCompletionProvider>();
    }
}
