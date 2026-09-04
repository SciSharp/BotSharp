using BotSharp.Abstraction.Plugins;
using BotSharp.Plugin.FloxiAI.Providers;
using BotSharp.Plugin.FloxiAI.Providers.Chat;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BotSharp.Plugin.FloxiAI;

/// <summary>
/// Models served by the floxi inference network (provider "floxi-ai"), reachable either over its
/// OpenAI-compatible HTTP API or, for a host that owns the network in-process, through its own
/// <see cref="IFloxiChatTransport"/>.
/// </summary>
public class FloxiAiPlugin : IBotSharpPlugin
{
    public string Id => "6b5e8f34-9d21-4c7e-a4f1-2b8a0c6d5e93";
    public string Name => "Floxi AI";
    public string Description => "Chat completion on the floxi inference network.";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IChatCompletion, ChatCompletionProvider>();

        // Idempotent, and the HTTP transport below cannot be constructed without it — a host that never
        // called it would otherwise only find out when the first completion is requested.
        services.AddHttpClient();

        // TryAddEnumerable, not Add: the HTTP transport is the fallback, and a host that registered its
        // own transport before the plugin loaded keeps it — the provider picks the highest priority of
        // whatever ends up registered, so both orders resolve to the host's transport.
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IFloxiChatTransport, HttpFloxiChatTransport>());
    }
}
