using BotSharp.Abstraction.Agents;
using BotSharp.Plugin.PizzaBot.Hooks;

namespace BotSharp.Plugin.PizzaBot;

public class PizzaBotPlugin : IBotSharpPlugin
{
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        // Register hooks
        services.AddScoped<IAgentHook, PizzaBotAgentHook>();
    }
}
