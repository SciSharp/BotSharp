using Microsoft.Extensions.Configuration;
using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.Settings;
using BotSharp.Core.MCP.Settings;
using BotSharp.Core.MCP.Services;

namespace BotSharp.Core.MCP;

public class McpPlugin : IBotSharpPlugin
{
    public string Id => "0cfb486a-229e-4470-a4c6-d2d4a5fdc727";
    public string Name => "Model context protocol";
    public string Description => "Model context protocol";

    public SettingsMeta Settings =>
        new SettingsMeta("MCP");

    public object GetNewSettingsInstance() =>
         new McpSettings();

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IMcpService, McpService>();
    }

    public bool AttachMenu(List<PluginMenuDef> menu)
    {
        return true;
    }
}
