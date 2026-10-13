using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Core.Plugins;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task<PluginDef> GetPlugin(string agentId)
    {
        var plugins = await GetPlugins();
        return MatchPlugin(plugins, agentId);
    }

    // One cache entry shared by all agents. The plugin admin page reads PluginLoader directly,
    // so installing or removing a plugin shows up there without waiting for this to expire.
    [SharpCache(10)]
    private async Task<List<PluginDef>> GetPlugins()
    {
        var loader = _services.GetRequiredService<PluginLoader>();
        return await loader.GetPlugins(_services);
    }

    private static PluginDef MatchPlugin(List<PluginDef> plugins, string agentId)
    {
        return plugins.FirstOrDefault(x => x.AgentIds.Contains(agentId)) ??
            new PluginDef
            {
                Id = Guid.Empty.ToString(),
                AgentIds = new[] 
                { 
                    agentId
                },
                Assembly = typeof(AgentService).Assembly.FullName.Split(',').First(),
                Name = "BotSharp",
                Enabled = true,
            };
    }
}
