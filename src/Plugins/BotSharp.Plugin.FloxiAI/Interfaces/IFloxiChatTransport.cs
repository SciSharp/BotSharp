namespace BotSharp.Plugin.FloxiAI.Interfaces;

/// <summary>
/// How a chat request reaches the floxi inference network. The plugin ships an HTTP transport that
/// posts to the OpenAI-compatible endpoint in the model's settings, which is what an application
/// outside the floxi service uses.
///
/// A host that already holds the network in-process (the floxi service itself, which owns the daemon
/// control plane) registers its own transport instead, so the request goes straight to a node rather
/// than back in through the host's own public API — no loopback hop, no second authorization, and no
/// double-counted usage. This seam is the reason the plugin has no dependency on the hub library.
/// </summary>
public interface IFloxiChatTransport
{
    /// <summary>
    /// Highest priority wins when several transports are registered. The bundled HTTP transport sits
    /// at 0 so any host-supplied transport takes over by declaring anything above it.
    /// </summary>
    int Priority => 0;

    /// <summary>
    /// Sends an OpenAI-shaped chat-completions body for <paramref name="model"/> and returns the reply
    /// verbatim. Implementations report a backend refusal through
    /// <see cref="FloxiChatTransportResult.StatusCode"/> rather than by throwing; throwing is reserved
    /// for the request never reaching a backend at all.
    /// </summary>
    Task<FloxiChatTransportResult> SendChatAsync(string model, string payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether this transport can deliver a reply incrementally. The bundled HTTP transport reads
    /// server-sent events, so it can; a control plane that carries one terminal reply per dispatch
    /// leaves this false and the provider streams that whole reply as a single chunk instead.
    /// </summary>
    bool SupportsStreaming => false;

    /// <summary>
    /// Sends a body carrying "stream": true and yields each event's data payload verbatim — one
    /// OpenAI chunk object per element, with the "[DONE]" terminator dropped. Only called when
    /// <see cref="SupportsStreaming"/> is true. Unlike <see cref="SendChatAsync"/> a refusal is
    /// thrown rather than returned, because a rejected request produces no stream to report it on.
    /// </summary>
    IAsyncEnumerable<string> SendChatStreamAsync(string model, string payload, CancellationToken cancellationToken = default)
        => throw new NotSupportedException(
            $"{GetType().Name} does not stream; check {nameof(SupportsStreaming)} before calling this.");
}
