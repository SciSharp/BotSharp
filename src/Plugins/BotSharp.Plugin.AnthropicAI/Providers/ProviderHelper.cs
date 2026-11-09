namespace BotSharp.Plugin.AnthropicAI.Providers;

public static class ProviderHelper
{
    public static AnthropicClient GetAnthropicClient(string provider, string model, IServiceProvider services)
    {
        var settingsService = services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(provider, model);
        var client = new AnthropicClient(new APIAuthentication(settings.ApiKey));
        if (!string.IsNullOrWhiteSpace(settings.Endpoint))
        {
            client.ApiUrlFormat = $"{settings.Endpoint.TrimEnd('/')}/{{0}}/{{1}}";
        }
        return client;
    }

    public const string MaxTokensMessage = "AI response exceeded max output length";

    /// <summary>
    /// Content of the final streamed message. end_turn keeps the collected text; max_tokens uses the same
    /// notice as the non-streaming path instead of the raw stop reason; other stop reasons are unchanged.
    /// </summary>
    public static string ResolveStreamedContent(string? stopReason, string collectedText)
    {
        return stopReason switch
        {
            StopReason.EndTurn => collectedText,
            StopReason.MaxTokens => MaxTokensMessage,
            _ => stopReason ?? string.Empty
        };
    }
}
