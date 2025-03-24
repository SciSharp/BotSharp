using LLMSharp.Google.Palm;

namespace BotSharp.Plugin.GoogleAi.Providers;

public static class ProviderHelper
{
    public static GenerativeAI.GoogleAi GetGeminiClient(string provider, string model, IServiceProvider services)
    {
        var settingsService = services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(provider, model);
        var client = new GenerativeAI.GoogleAi(settings.ApiKey);
        return client;
    }

    public static GooglePalmClient GetPalmClient(string provider, string model, IServiceProvider services)
    {
        var settingsService = services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(provider, model);
        var client = new GooglePalmClient(settings.ApiKey);
        return client;
    }
}
