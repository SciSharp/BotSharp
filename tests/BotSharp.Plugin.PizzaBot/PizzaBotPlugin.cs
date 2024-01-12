using BotSharp.Abstraction.Agents;
using BotSharp.Plugin.PizzaBot.Hooks;

namespace BotSharp.Plugin.PizzaBot;

public class PizzaBotPlugin : IBotSharpPlugin
{
    public string Id => "1c8270eb-de63-4ca0-8903-654d83ce5ece";
    public string Name => "Pizza AI Assistant";
    public string Description => "An example of an enterprise-grade AI Chatbot.";
    public string IconUrl => "https://cdn-icons-png.flaticon.com/512/6978/6978255.png";
    public bool WithAgent => true;

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        // Register hooks
        services.AddScoped<IAgentHook, PizzaBotAgentHook>();
    }
}
