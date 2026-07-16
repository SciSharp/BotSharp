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
    string[] AgentIds => [];

    void RegisterDI(IServiceCollection services, IConfiguration config);

    bool AttachMenu(List<PluginMenuDef> menu) => true;

    /// <summary>
    /// Service-aware menu attachment invoked with the request-scoped service provider,
    /// so plugins can build per-user menu entries. Defaults to the sync overload.
    /// </summary>
    Task<bool> AttachMenu(List<PluginMenuDef> menu, IServiceProvider services)
        => Task.FromResult(AttachMenu(menu));
}
