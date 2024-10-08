using BotSharp.Abstraction.Plugins.Models;

namespace BotSharp.Plugin.EntityFrameworkCore.Repository;

public partial class EfCoreRepository
{
    #region Plugin
    public PluginConfig GetPluginConfig()
    {
        var config = new PluginConfig();
        var found = _context.Plugins.FirstOrDefault();
        if (found != null)
        {
            config = new PluginConfig()
            {
                EnabledPlugins = found.EnabledPlugins
            };
        }
        return config;
    }

    public void SavePluginConfig(PluginConfig config)
    {
        if (config == null || config.EnabledPlugins == null) return;

        var plugin = _context.Plugins.FirstOrDefault();

        if (plugin != null)
        {
            plugin.EnabledPlugins = config.EnabledPlugins;
            _context.Plugins.Update(plugin);
        }
        else
        {
            plugin = new Entities.Plugin
            {
                Id = Guid.NewGuid().ToString(),
                EnabledPlugins = config.EnabledPlugins
            };
            _context.Plugins.Add(plugin);
        }
        _context.SaveChanges();
    }
    #endregion
}
