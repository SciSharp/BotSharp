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
}
