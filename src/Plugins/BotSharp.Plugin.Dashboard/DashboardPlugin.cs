using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Settings;
using BotSharp.Abstraction.Statistics.Settings;
using BotSharp.Plugin.Dashboard.Hooks;

namespace BotSharp.Plugin.Dashboard;

public class DashboardPlugin : IBotSharpPlugin
{
    public string Id => "d42a0c21-b461-44f6-ada2-499510d260af";
    public string Name => "Dashboard";
    public string Description => "Dashboard that offers real-time statistics on model performance, usage trends, and user feedback";
    public string IconUrl => "https://cdn0.iconfinder.com/data/icons/octicons/1024/dashboard-512.png";
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IConversationHook, StatsConversationHook>();
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<StatisticsSettings>("Statistics");
        });
    }

    public bool AttachMenu(List<PluginMenuDef> menu)
    {
        var section = menu.First(x => x.Label == "Apps");
        menu.Add(new PluginMenuDef("Dashboard", link: "page/dashboard", icon: "bx bx-home-circle", weight: section.Weight - 1));
        return true;
    }
}
