namespace BotSharp.Plugin.GoogleAi.Providers;

public static class ProviderHelper
{
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

    /// <summary>
    /// Tokens served from the implicit/explicit context cache. Gemini includes
    /// them in <c>PromptTokenCount</c>, so they are billed at the cached rate.
    /// </summary>
    public static long GetCachedInputTokens(GenerativeAI.Types.UsageMetadata? usage)
    {
        return usage?.CachedContentTokenCount ?? 0;
    }

    /// <summary>
    /// Prompt tokens billed at the full input price (prompt minus cache hits).
    /// </summary>
    public static long GetTextInputTokens(GenerativeAI.Types.UsageMetadata? usage)
    {
        var uncached = (usage?.PromptTokenCount ?? 0) - GetCachedInputTokens(usage);
        return uncached > 0 ? uncached : 0;
    }
}
