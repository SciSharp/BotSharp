namespace BotSharp.Plugin.AgentTesting.Services;

/// <summary>
/// Puts "talking to a BotSharp conversation" behind a seam, which is what makes the runner's
/// orchestration unit-testable at all -- otherwise testing multi-turn orchestration once would
/// require a real Mongo and a real model.
/// </summary>
public interface IAgentConversationDriver
{
    Task PrepareAsync(string conversationId, string agentId, IReadOnlyList<TestState> initialStates);

    /// <summary>
    /// Writes authored history into the conversation before any turn runs, and returns how many of
    /// those messages are actually readable back out of the store.
    ///
    /// The return value is the point. IBotSharpRepository.AppendConversationDialogs is an UpdateOne
    /// with no upsert, so it silently does nothing when the conversation's dialog document does not
    /// exist yet -- and PrepareAsync deliberately does not create it (the row is created by the
    /// first SendMessage). A case whose history vanished would run against no context at all and
    /// still report Passed, so the caller compares this count against what it asked for.
    /// </summary>
    Task<int> InjectHistoryAsync(
        string conversationId, string agentId, IReadOnlyList<TestHistoryMessage> history);

    /// <summary>Drives one turn and returns that turn's output text.</summary>
    Task<string?> SendAsync(string conversationId, string agentId, string userMessage, CancellationToken ct);

    /// <summary>Calls the canary function once; returns whether the mock seam took it over.</summary>
    Task<bool> RunCanaryAsync(string conversationId, string agentId, CancellationToken ct);

    Task<IReadOnlyDictionary<string, string?>> ReadStatesAsync(string conversationId);

    /// <summary>
    /// The agent behind every assistant message so far, in dialog order, NOT de-duplicated and NOT
    /// sliced per turn -- the caller does both, because only it knows where each turn began.
    ///
    /// Replaces an earlier ReadRoutedAgentNameAsync that returned just the last assistant message's
    /// agent. That was the only routing signal available and it could not describe a hand-off: for
    /// Entry -> A -> B it reported B alone, and when control returned to the entry agent and that
    /// agent emitted the closing message it reported the entry agent, failing a correctly routed
    /// case. Returning the sequence lets the runner derive both the last agent and the chain from
    /// one read, so they cannot disagree, and costs no extra round trip.
    /// </summary>
    Task<IReadOnlyList<AgentChainHop>> ReadAssistantAgentSequenceAsync(string conversationId);
}
