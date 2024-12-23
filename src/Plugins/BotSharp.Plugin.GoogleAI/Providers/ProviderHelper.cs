using LLMSharp.Google.Palm;
using Mscc.GenerativeAI;

namespace BotSharp.Plugin.GoogleAi.Providers;

public static class ProviderHelper
{
    public static GoogleAI GetGeminiClient(IServiceProvider services)
    {
        var settings = services.GetRequiredService<GoogleAiSettings>();
        var client = new GoogleAI(settings.Gemini.ApiKey);
        return client;
    }

    public static GooglePalmClient GetPalmClient(IServiceProvider services)
    {
        var settings = services.GetRequiredService<GoogleAiSettings>();
        var client = new GooglePalmClient(settings.PaLM.ApiKey);
        return client;
    }
}
