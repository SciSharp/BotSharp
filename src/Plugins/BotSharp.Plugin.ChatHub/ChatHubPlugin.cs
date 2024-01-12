using BotSharp.Abstraction.Loggers;
using BotSharp.Plugin.ChatHub.Hooks;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.ChatHub;

/// <summary>
/// The dialogue channel connects users, AI assistants and customer service representatives.
/// </summary>
public class ChatHubPlugin : IBotSharpPlugin
{
    public string Id => "6e52d42d-1e23-406b-8599-36af36c83209";
    public string Name => "Chat Hub";
    public string Description => "A communication channel connects agents and users in real-time.";
    public string IconUrl => "https://media.zeemly.com/media/product/chatrandom.png";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        // Register hooks
        services.AddScoped<IConversationHook, ChatHubConversationHook>();
        services.AddScoped<IContentGeneratingHook, StreamingLogHook>();
    }
}
