using BotSharp.Plugin.ChatHub.Hooks;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.ChatHub;

/// <summary>
/// The dialogue channel connects users, AI assistants and customer service representatives.
/// </summary>
public class ChatHubPlugin : IBotSharpPlugin
{
    public string Name => "Chat Hub";
    public string Description => "A communication channel connects agents and users in real-time.";
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        // Register hooks
        services.AddScoped<IConversationHook, ChatHubConversationHook>();
    }
}
