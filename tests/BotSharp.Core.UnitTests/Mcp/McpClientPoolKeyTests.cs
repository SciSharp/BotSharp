using BotSharp.Core.MCP.Managers;
using Xunit;

namespace BotSharp.Core.UnitTests.Mcp;

/// <summary>
/// Pins down the pool key that decides which callers may share an MCP connection.
///
/// The manager is registered per DI scope, so in practice a pool only ever holds one caller's
/// connections and identities cannot mix. The key is the second line of defence: it folds in the
/// headers the connection opens with, so the guarantee still holds if someone later registers the
/// manager with a longer lifetime. These tests exist so that property cannot be quietly lost --
/// getting it wrong hands one user's connection, and therefore one user's credential, to another.
/// </summary>
public class McpClientPoolKeyTests
{
    private const string ServerId = "sumo-logic";

    [Fact]
    public void SameHeaders_ShareOneEntry()
    {
        var a = McpClientManager.BuildPoolKey(ServerId, new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer alice-token",
            ["X-Tenant"] = "lessen"
        });

        // Same pairs, different insertion order: the key is canonicalised, so these are one entry.
        var b = McpClientManager.BuildPoolKey(ServerId, new Dictionary<string, string>
        {
            ["X-Tenant"] = "lessen",
            ["Authorization"] = "Bearer alice-token"
        });

        Assert.Equal(a, b);
    }

    [Fact]
    public void DifferentCredential_NeverSharesAnEntry()
    {
        var alice = McpClientManager.BuildPoolKey(ServerId, new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer alice-token"
        });

        var bob = McpClientManager.BuildPoolKey(ServerId, new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer bob-token"
        });

        Assert.NotEqual(alice, bob);
    }

    /// <summary>
    /// The three identities OneBrainMcpHeaderProvider can answer with -- the caller's own token,
    /// the credential configured for the server, and X-API-KEY minted from a user id -- are
    /// different callers, not interchangeable ways of naming one. None may share a connection.
    /// </summary>
    [Fact]
    public void DifferentIdentityKinds_NeverShareAnEntry()
    {
        var callerToken = McpClientManager.BuildPoolKey(ServerId, new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer caller-token"
        });

        var configuredCredential = McpClientManager.BuildPoolKey(ServerId, new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer service-credential"
        });

        var mintedApiKey = McpClientManager.BuildPoolKey(ServerId, new Dictionary<string, string>
        {
            ["X-API-KEY"] = "mesh-key-42"
        });

        Assert.Equal(3, new HashSet<string> { callerToken, configuredCredential, mintedApiKey }.Count);
    }

    [Fact]
    public void SameHeaders_DifferentServers_DoNotShareAnEntry()
    {
        var headers = new Dictionary<string, string> { ["Authorization"] = "Bearer alice-token" };

        Assert.NotEqual(
            McpClientManager.BuildPoolKey("sumo-logic", headers),
            McpClientManager.BuildPoolKey("meshstage", headers));
    }

    /// <summary>
    /// A stdio server is not consulted for headers, and an http server may simply have none
    /// configured. Both land on one stable entry per server, distinct from any authenticated one.
    /// </summary>
    [Fact]
    public void NoHeaders_IsStable_AndDistinctFromAuthenticated()
    {
        var fromNull = McpClientManager.BuildPoolKey(ServerId, null);
        var fromEmpty = McpClientManager.BuildPoolKey(ServerId, new Dictionary<string, string>());
        var authenticated = McpClientManager.BuildPoolKey(ServerId, new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer alice-token"
        });

        Assert.Equal(fromNull, fromEmpty);
        Assert.NotEqual(fromNull, authenticated);
    }

    /// <summary>
    /// The key is derived from credentials, so it must not carry one. Keys reach logs and dumps by
    /// accident far more easily than the headers themselves do.
    /// </summary>
    [Fact]
    public void Key_DoesNotCarryTheCredential()
    {
        const string secret = "Bearer alice-super-secret-token";

        var key = McpClientManager.BuildPoolKey(ServerId, new Dictionary<string, string>
        {
            ["Authorization"] = secret
        });

        Assert.DoesNotContain("alice-super-secret-token", key);
        Assert.DoesNotContain(secret, key);
        Assert.StartsWith(ServerId + "|", key);
    }
}
