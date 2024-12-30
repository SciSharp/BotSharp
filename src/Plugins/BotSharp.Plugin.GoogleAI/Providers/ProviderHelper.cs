using LLMSharp.Google.Palm;
using Mscc.GenerativeAI;

namespace BotSharp.Plugin.GoogleAi.Providers;

public static class ProviderHelper
{
    public static GoogleAI GetGeminiClient(string provider, string model, IServiceProvider services)
    {
        var settingsService = services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(provider, model);
        var client = new GoogleAI(settings.ApiKey);
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
