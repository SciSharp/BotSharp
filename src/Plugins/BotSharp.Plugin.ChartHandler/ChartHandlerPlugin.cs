using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.ChartHandler;

public class ChartHandlerPlugin : IBotSharpPlugin
{
    public string Id => "9dacac1d-2e29-4f01-9d66-b0201f05a9fa";
    public string Name => "Chart Plotter";
    public string Description => "AI plots chart";
    public string IconUrl => "https://cdn-icons-png.flaticon.com/512/423/423786.png";
    public string[] AgentIds => [];

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IAgentUtilityHook, ChartHandlerUtilityHook>();
    }

}
