namespace BotSharp.Abstraction.MCP.Services;

/// <summary>
/// An optional host hook over the HTTP headers used to open one MCP server connection.
/// </summary>
/// <remarks>
/// Nothing registers this by default. With no implementation registered the headers configured
/// under <c>MCP:McpServerConfigs</c> are used verbatim, which is the only behaviour there was
/// before the hook existed — a host that does not implement it sees no change at all.
/// <para>
/// It exists so a host can call a server as whoever is driving the conversation instead of with
/// one fixed credential. That decision belongs to the host: it is the only side that knows what
/// a caller's credential is and which servers may be shown it.
/// </para>
/// </remarks>
public interface IMcpClientHeaderProvider
{
    /// <summary>
    /// Answers the headers to send to <paramref name="serverId"/>.
    /// </summary>
    /// <param name="configured">
    /// The headers from configuration. This dictionary is shared for the lifetime of the process,
    /// so an implementation that changes a header MUST copy it rather than write into it.
    /// </param>
    /// <returns>
    /// The headers to send. Returning <paramref name="configured"/> unchanged is the no-op answer,
    /// and is the answer expected for any server the implementation does not recognise.
    /// </returns>
    Dictionary<string, string>? GetHeaders(string serverId, Dictionary<string, string>? configured);
}
