using BotSharp.Abstraction.Plugins.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    #region Plugin
    public async Task<PluginConfig> GetPluginConfig()
    {
        var config = new PluginConfig();
        var found = await _dc.Plugins.Find(Builders<PluginDocument>.Filter.Empty).FirstOrDefaultAsync();
        if (found != null)
        {
            config = new PluginConfig()
            {
                EnabledPlugins = found.EnabledPlugins
            };
        }
        return config;
    }

    public async Task SavePluginConfig(PluginConfig config)
    {
        if (config == null || config.EnabledPlugins == null) return;

        var filter = Builders<PluginDocument>.Filter.Empty;
        var update = Builders<PluginDocument>.Update
            .Set(x => x.EnabledPlugins, config.EnabledPlugins)
            .SetOnInsert(x => x.Id, Guid.NewGuid().ToString());

        await _dc.Plugins.UpdateOneAsync(filter, update, _options);
    }
    #endregion
}
