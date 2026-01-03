using DeepSeek.Core;
using System.Threading;

namespace BotSharp.Plugin.DeepSeek.Providers;

public static class ProviderHelper
{
    public static DeepSeekClient GetClient(string provider, string model, IServiceProvider services)
    {
        var settingsService = services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(provider, model);
        return new DeepSeekClient(settings.ApiKey);
    }
}
