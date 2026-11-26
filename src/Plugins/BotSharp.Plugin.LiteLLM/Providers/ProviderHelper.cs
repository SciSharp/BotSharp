using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace BotSharp.Plugin.LiteLLM.Providers;

public static class ProviderHelper
{
    public const string MaxTokensMessage = "AI response exceeded max output length";

    /// <summary>
    /// Content of the final streamed message. A reply cut off by the output limit gets the same notice as the
    /// non-streaming path instead of silently passing the truncated text off as a complete answer.
    /// </summary>
    public static string ResolveStreamedContent(ChatFinishReason reason, string collectedText)
    {
        return reason == ChatFinishReason.Length ? MaxTokensMessage : collectedText;
    }

    public static OpenAIClient GetClient(string provider, string model, IServiceProvider services)
    {
        var settingsService = services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(provider, model);
        var options = !string.IsNullOrEmpty(settings.Endpoint) ?
                        new OpenAIClientOptions { Endpoint = new Uri(settings.Endpoint) } : null;
        return new OpenAIClient(new ApiKeyCredential(settings.ApiKey), options);
    }
}
