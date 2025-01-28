using OpenAI;
using System.ClientModel;

namespace BotSharp.Plugin.DeepSeek.Providers;

public static class ProviderHelper
{
    public static OpenAIClient GetClient(string provider, string model, IServiceProvider services)
    {
        var settingsService = services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(provider, model);
        var options = !string.IsNullOrEmpty(settings.Endpoint) ?
                        new OpenAIClientOptions { Endpoint = new Uri(settings.Endpoint) } : null;
        return new OpenAIClient(new ApiKeyCredential(settings.ApiKey), options);
    }
}
