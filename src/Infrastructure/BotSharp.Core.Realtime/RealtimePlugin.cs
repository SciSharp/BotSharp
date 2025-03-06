using BotSharp.Abstraction.Plugins;
using BotSharp.Core.Realtime.Hooks;
using BotSharp.Core.Realtime.Services;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.Realtime;

public class RealtimePlugin : IBotSharpPlugin
{
    public string Id => "68c1c737-5c21-49de-b141-cd5c8d9bf978";
    public string Name => "Realtime Hub";
    public string? IconUrl => "https://thumbs.dreamstime.com/b/microphone-icon-sound-waves-voice-command-recording-message-sign-349007898.jpg";
    public string Description => "Build low-latency, multi-modal experiences with the Realtime API.";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IRealtimeHub, RealtimeHub>();
        services.AddScoped<IConversationHook, RealtimeConversationHook>();
    }
}
