using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Plugin.MongoStorage.Collections;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    #region Plugin
    public PluginConfig GetPluginConfig()
    {
        var config = new PluginConfig();
        var found = _dc.Plugins.AsQueryable().FirstOrDefault();
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

        var filter = Builders<PluginDocument>.Filter.Empty;
        var update = Builders<PluginDocument>.Update
            .Set(x => x.EnabledPlugins, config.EnabledPlugins)
            .SetOnInsert(x => x.Id, Guid.NewGuid().ToString());

        _dc.Plugins.UpdateOne(filter, update, _options);
    }
    #endregion
}
