using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Settings;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.HttpHandler;

public class HttpHandlerPlugin : IBotSharpPlugin
{
    public string Id => "2c1eb1c4-16e5-4c65-8ee4-032324c26b81";
    public string Name => "HTTP Handler";
    public string Description => "Empower agent to handle HTTP request in RESTful API or GraphQL";
    public string IconUrl => "https://lirp.cdn-website.com/6f8d6d8a/dms3rep/multi/opt/API_Icon-640w.png";
    public string[] AgentIds => new[] { "87c458fc-ec5f-40ae-8ed6-05dda8a07523" };

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<HttpHandlerSettings>("HttpHandler");
        });

        services.AddScoped<IAgentHook, HttpHandlerHook>();
        services.AddScoped<IAgentUtilityHook, HttpHandlerUtilityHook>();
    }
}
