using OpenAI;
using System.ClientModel;

namespace BotSharp.Plugin.Daoxe.Providers;

public static class ProviderHelper
{
    public const string DEFAULT_ENDPOINT = "https://api.daoxe.com/v1/";

    public static OpenAIClient GetClient(string provider, string model, IServiceProvider services)
    {
        var settingsService = services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(provider, model);
        var endpoint = !string.IsNullOrEmpty(settings.Endpoint) ? settings.Endpoint : DEFAULT_ENDPOINT;
        var options = new OpenAIClientOptions { Endpoint = new Uri(endpoint) };
        return new OpenAIClient(new ApiKeyCredential(settings.ApiKey), options);
    }
}
