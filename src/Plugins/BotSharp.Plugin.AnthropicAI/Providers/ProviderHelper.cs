namespace BotSharp.Plugin.AnthropicAI.Providers;

public static class ProviderHelper
{
    public static AnthropicClient GetAnthropicClient(string provider, string model, IServiceProvider services)
    {
        var settingsService = services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(provider, model);
        var client = new AnthropicClient(new APIAuthentication(settings.ApiKey));
        return client;
    }
}
