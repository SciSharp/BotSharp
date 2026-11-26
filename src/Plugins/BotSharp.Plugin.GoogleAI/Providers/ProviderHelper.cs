using GenerativeAI.Types;

namespace BotSharp.Plugin.GoogleAi.Providers;

public static class ProviderHelper
{
    public const string MaxTokensMessage = "AI response exceeded max output length";

    /// <summary>
    /// Content saved when a streamed Gemini reply ends for a reason other than STOP.
    /// MAX_TOKENS uses the same notice as the non-streaming path; every other reason
    /// keeps FinishMessage, or the reason name when FinishMessage is absent.
    /// </summary>
    public static string ResolveStreamedContent(FinishReason reason, string? finishMessage)
    {
        return reason == FinishReason.MAX_TOKENS
            ? MaxTokensMessage
            : finishMessage ?? reason.ToString();
    }

    public static GenerativeAI.GoogleAi GetGeminiClient(string provider, string model, IServiceProvider services)
    {
        var aiSettings = services.GetRequiredService<GoogleAiSettings>();
        if (string.IsNullOrEmpty(aiSettings?.Gemini?.ApiKey))
        {
            var settingsService = services.GetRequiredService<ILlmProviderService>();
            var settings = settingsService.GetSetting(provider, model);
            var client = new GenerativeAI.GoogleAi(settings.ApiKey);
            return client;
        }
        else
        {
            return new GenerativeAI.GoogleAi(aiSettings.Gemini.ApiKey);
        }
    }

    /// <summary>
    /// Gemini bills thinking tokens as output but reports them separately from
    /// the candidates, so both have to be counted.
    /// </summary>
    public static long GetOutputTokens(GenerativeAI.Types.UsageMetadata? usage)
    {
        return (usage?.CandidatesTokenCount ?? 0) + (usage?.ThoughtsTokenCount ?? 0);
    }
}
