using BotSharp.Abstraction.Settings;
using BotSharp.Core.Plugins;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.Infrastructures;

public class SettingService : ISettingService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private readonly ILogger _logger;

    public SettingService(IServiceProvider services, 
        IConfiguration config,
        ILogger<SettingService> logger)
    {
        _services = services;
        _config = config;
    }

    public T Bind<T>(string path) where T : new()
    {
        var settings = new T();
        _config.Bind(path, settings);
        return settings;
    }

    public object GetDetail(string settingName, bool mask = false)
    {
        var pluginService = _services.GetRequiredService<PluginLoader>();
        var plugins = pluginService.GetPlugins(_services);
        var plugin = plugins.First(x => x.Module.Settings.Name == settingName);
        var instance = plugin.Module.GetNewSettingsInstance();

        _config.Bind(settingName, instance);
        if (mask)
        {
            plugin.Module.MaskSettings(instance);
        }
        return instance;
    }

    public static string Mask(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }
        value = value.Substring(0, value.Length / 2 - 1) 
            + string.Join("", Enumerable.Repeat("*", value.Length / 2));
        return value;
    }
}
