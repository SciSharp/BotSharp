using BotSharp.Abstraction.Agents;
using BotSharp.Plugin.PizzaBot.Hooks;

namespace BotSharp.Plugin.PizzaBot;

public class PizzaBotPlugin : IBotSharpPlugin
{
    public string Id => "1c8270eb-de63-4ca0-8903-654d83ce5ece";
    public string Name => "Pizza AI Assistant";
    public string Description => "An example of an enterprise-grade AI Chatbot.";
    public string IconUrl => "https://cdn-icons-png.flaticon.com/512/6978/6978255.png";
    public string[] AgentIds => new[] 
    {
        "8970b1e5-d260-4e2c-90b1-f1415a257c18", // Pizza Bot.  type: routing, planner: NaviePlanner (NaivePlanner)
        "b284db86-e9c2-4c25-a59e-4649797dd130", // Order Inquiry.  redirectTo: Ordering.
        "c2b57a74-ae4e-4c81-b3ad-9ac5bff982bd", // Ordering.
        "dfd9b46d-d00c-40af-8a75-3fbdc2b89869", // Evaluator. From Infrastructure/Core
        "fe8c60aa-b114-4ef3-93cb-a8efeac80f75" // Payment.  redirectTo: Ordering.
    };

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        // Register hooks
        services.AddScoped<IAgentHook, PizzaBotAgentHook>();
    }
}
