using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.MLTasks.Settings;

namespace BotSharp.Core.Infrastructures;

public class LlmProviderSettingService : ILlmProviderSettingService
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public LlmProviderSettingService(IServiceProvider services, ILogger<LlmProviderSettingService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public LlmModelSetting? GetSetting(string provider, string model)
    {
        var settings = _services.GetRequiredService<List<LlmProviderSetting>>();
        var providerSetting = settings.FirstOrDefault(p => p.Provider.Equals(provider, StringComparison.CurrentCultureIgnoreCase));
        if (providerSetting == null)
        {
            _logger.LogError($"Can't find provider settings for {provider}");
            return null;
        }

        var modelSetting = providerSetting.Models.FirstOrDefault(m => m.Name.Equals(model, StringComparison.CurrentCultureIgnoreCase));
        if (modelSetting == null)
        {
            _logger.LogError($"Can't find model settings for {provider}.{model}");
            return null;
        }

        return modelSetting;
    }
}
