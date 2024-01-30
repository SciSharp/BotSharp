using BotSharp.Abstraction.MLTasks.Settings;
using BotSharp.Abstraction.Settings;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.Infrastructures;

public class LlmProviderPlugin : IBotSharpPlugin
{
    public string Id => "0c52c0e3-cbb9-48ab-9381-260b80f018b8";
    public string Name => "LLM Provider";
    public string Description => "This plugin allows you to configure multiple different model providers and switch to different models as needed.";

    public SettingsMeta Settings => 
        new SettingsMeta("LlmProviders");

    public object GetNewSettingsInstance() =>
         new List<LlmProviderSetting>();

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            var loger = provider.GetRequiredService<ILogger<LlmProviderPlugin>>();
            var llmProviders = settingService.Bind<List<LlmProviderSetting>>("LlmProviders");
            foreach (var llmProvider in llmProviders)
            {
                loger.LogInformation($"Loaded LlmProvider {llmProvider.Provider} settings with {llmProvider.Models.Count} models.");
            }
            return llmProviders;
        });
    }

    public bool MaskSettings(object settings)
    {
        if (settings is List<LlmProviderSetting> instance)
        {
            foreach (var item in instance)
            {
                foreach (var model in item.Models)
                {
                    model.Endpoint = SettingService.Mask(model.Endpoint);
                    model.ApiKey = SettingService.Mask(model.ApiKey);
                }
            }
        }
        return true;
    }
}
