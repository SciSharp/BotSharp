using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Plugin.RoutingSpeeder;

public class RoutingSpeederPlugin : IBotSharpPlugin
{
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IConversationHook, RoutingConversationHook>();
    }
}
