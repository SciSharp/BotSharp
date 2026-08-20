namespace BotSharp.Plugin.AgentTesting.Services;

/// <summary>
/// Everything an assertion is evaluated against. Turn-level and case-level share this shape and
/// differ only in how much of it is populated.
/// </summary>
public class AssertionContext
{
    public string? Output { get; set; }
    public IReadOnlyList<ObservedToolCall> ToolCalls { get; set; } = [];
    public IReadOnlyDictionary<string, string?> States { get; set; } = new Dictionary<string, string?>();

    /// <summary>
    /// Which agents answered, in order, consecutive repeats collapsed. Turn-level contexts carry only
    /// that turn's slice; the case-level context carries the whole conversation.
    ///
    /// There is deliberately no separate "routed to agent" field: routedToAgent is this chain's last
    /// entry, and two fields fed from one read are two things that can drift apart.
    /// </summary>
    public IReadOnlyList<AgentChainHop> AgentChain { get; set; } = [];
}

/// <summary>
/// One agent in a chain, carrying BOTH identifiers on purpose.
///
/// An assertion has to accept either. The name is what a human reads and what the UI shows, but it
/// is also mutable -- renaming an agent would silently break every routing case asserting on it. The
/// id is stable but is a guid nobody recognises, and it is exactly what an author copies out of the
/// agent list, which is how the first real routing case came to assert an id against a name and could
/// never pass.
///
/// Not persisted: <see cref="AgentTestCaseResult.AgentChain"/> stores names, because that is what a
/// person reads off a result.
/// </summary>
public class AgentChainHop
{
    public string Id { get; set; } = string.Empty;

    /// <summary>Falls back to the id when the agent cannot be loaded, so this is never blank.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether a token an author typed refers to this hop. Case-insensitive, and matches the id just
    /// as readily as the name.
    /// </summary>
    public bool Matches(string token)
        => string.Equals(Name, token, StringComparison.OrdinalIgnoreCase)
        || string.Equals(Id, token, StringComparison.OrdinalIgnoreCase);
}
