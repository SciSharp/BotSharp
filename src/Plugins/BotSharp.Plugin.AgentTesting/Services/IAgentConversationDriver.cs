namespace BotSharp.Plugin.AgentTesting.Services;

/// <summary>
/// Puts "talking to a BotSharp conversation" behind a seam, which is what makes the runner's
/// orchestration unit-testable at all -- otherwise testing multi-turn orchestration once would
/// require a real Mongo and a real model.
/// </summary>
public interface IAgentConversationDriver
{
    Task PrepareAsync(string conversationId, string agentId, IReadOnlyList<TestState> initialStates);

    /// <summary>Drives one turn and returns that turn's output text.</summary>
    Task<string?> SendAsync(string conversationId, string agentId, string userMessage, CancellationToken ct);

    /// <summary>Calls the canary function once; returns whether the mock seam took it over.</summary>
    Task<bool> RunCanaryAsync(string conversationId, string agentId, CancellationToken ct);

    Task<IReadOnlyDictionary<string, string?>> ReadStatesAsync(string conversationId);

    Task<string?> ReadRoutedAgentNameAsync(string conversationId);
}
