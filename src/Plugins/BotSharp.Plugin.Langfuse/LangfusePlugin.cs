using BotSharp.Abstraction.Loggers;
using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.Settings;
using BotSharp.Plugin.Langfuse.Settings;
using BotSharp.Plugin.Langfuse.Hooks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using zborek.Langfuse;
using zborek.Langfuse.Config;

namespace BotSharp.Plugin.Langfuse;

public class LangfusePlugin : IBotSharpPlugin
{
    public string Id => "a92f8c54-9c4a-4c3b-8b5d-7e8f9a0b1c2d";
    public string Name => "Langfuse Observability";
    public string Description => "Provides LLM observability and analytics through Langfuse platform integration";
    public SettingsMeta Settings => new SettingsMeta("Langfuse");

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        // Register BotSharp specific settings
        var langfuseSettings = new LangfuseSettings();
        config.Bind("Langfuse", langfuseSettings);
        services.AddSingleton(langfuseSettings);

        // Only register Langfuse services if enabled and properly configured
        if (langfuseSettings.Enabled && 
            !string.IsNullOrEmpty(langfuseSettings.PublicKey) && 
            !string.IsNullOrEmpty(langfuseSettings.SecretKey))
        {
            // Create Langfuse configuration section if it doesn't exist
            var langfuseConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Langfuse:PublicKey"] = langfuseSettings.PublicKey,
                    ["Langfuse:SecretKey"] = langfuseSettings.SecretKey,
                    ["Langfuse:Url"] = langfuseSettings.Host,
                    ["Langfuse:BatchMode"] = "false"  // Use synchronous mode for simplicity
                })
                .Build();

            // Register Langfuse services using their extension method
            services.AddLangfuse(langfuseConfig);
        }

        // Register the content generating hook
        services.AddScoped<IContentGeneratingHook, LangfuseContentGeneratingHook>();
    }

    public object GetNewSettingsInstance()
    {
        return new LangfuseSettings();
    }

    public bool MaskSettings(object settings)
    {
        if (settings is LangfuseSettings langfuseSettings)
        {
            // Mask sensitive information
            if (!string.IsNullOrEmpty(langfuseSettings.SecretKey))
            {
                langfuseSettings.SecretKey = "***masked***";
            }
            return true;
        }
        return false;
    }
}