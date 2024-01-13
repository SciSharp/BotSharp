using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Core.Plugins;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public PluginDef GetPlugin(string agentId)
    {
        var loader = _services.GetRequiredService<PluginLoader>();
        var plugins = loader.GetPlugins(_services);
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
