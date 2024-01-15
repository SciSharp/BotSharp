using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Abstraction.Plugins;

public interface IBotSharpPlugin
{
    /// <summary>
    /// Plugin id (guid)
    /// </summary>
    string Id { get; }
    string Name => "";
    string Description => "";
    SettingsMeta Settings => new SettingsMeta("");
    object GetNewSettingsInstance() => new object();
    bool MaskSettings(object settings) => true;
    string? IconUrl => null;

    /// <summary>
    /// Has build-in agent profile with this plugin
    /// </summary>
    string[] AgentIds => new string[0];

    void RegisterDI(IServiceCollection services, IConfiguration config);

    bool AttachMenu(List<PluginMenuDef> menu) => true;
}
