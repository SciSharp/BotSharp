using BotSharp.Abstraction.Agents;
using BotSharp.Plugin.PizzaBot.Hooks;

namespace BotSharp.Plugin.PizzaBot;

public class PizzaBotPlugin : IBotSharpPlugin
{
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        // Register callback function
        services.AddScoped<IFunctionCallback, GetPizzaTypesFn>();
        services.AddScoped<IFunctionCallback, GetPizzaPricesFn>();
        services.AddScoped<IFunctionCallback, MakeOrderFn>();
        services.AddScoped<IFunctionCallback, OrderFoundFn>();

        // Register hooks
        services.AddScoped<IAgentHook, PizzaBotAgentHook>();
    }
}
