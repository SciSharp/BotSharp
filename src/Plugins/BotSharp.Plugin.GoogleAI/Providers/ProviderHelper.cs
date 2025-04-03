using LLMSharp.Google.Palm;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.GoogleAi.Providers;

public static class ProviderHelper
{
    public static GenerativeAI.GoogleAi GetGeminiClient(string provider, string model, IServiceProvider services, GoogleAiSettings? aiSettings, ILogger? _logger)
    {
        if (aiSettings == null || aiSettings.Gemini ==null || string.IsNullOrEmpty(aiSettings.Gemini.ApiKey))
        {
            var settingsService = services.GetRequiredService<ILlmProviderService>();
            var settings = settingsService.GetSetting(provider, model);
            var client = new GenerativeAI.GoogleAi(settings.ApiKey, logger:_logger);
            return client;
        }
        else
        {
            return new GenerativeAI.GoogleAi(aiSettings.Gemini.ApiKey, logger:_logger);
        }
        
    }

    public static GooglePalmClient GetPalmClient(string provider, string model, IServiceProvider services)
    {
        var settingsService = services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(provider, model);
        var client = new GooglePalmClient(settings.ApiKey);
        return client;
    }
}
