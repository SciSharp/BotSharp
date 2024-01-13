using BotSharp.Abstraction.Plugins.Models;
using System.IO;

namespace BotSharp.Core.Repository;

public partial class FileRepository
{
    public PluginConfig GetPluginConfig()
    {
        if (_pluginConfig != null)
        {
            return _pluginConfig;
        }

        var dir = Path.Combine(_dbSettings.FileRepository, "plugins");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        var configFile = Path.Combine(dir, "config.json");
        if (!File.Exists(configFile))
        {
            _pluginConfig = new PluginConfig();
            return _pluginConfig;
        }

        var json = File.ReadAllText(configFile);
        _pluginConfig = JsonSerializer.Deserialize<PluginConfig>(json, _options);

        return _pluginConfig;
    }

    public void SavePluginConfig(PluginConfig config)
    {
        var configFile = Path.Combine(_dbSettings.FileRepository, "plugins", "config.json");
        File.WriteAllText(configFile, JsonSerializer.Serialize(config, _options));
        _pluginConfig = null;
    }
}
