namespace BotSharp.Plugin.AgentTesting.Services;

/// <summary>
/// 把"与 BotSharp 会话交互"这件事隔在一层后面，运行器的编排逻辑才可能被单元测试覆盖
/// （否则测一次多轮编排就要连真 Mongo + 真模型）。
/// </summary>
public interface IAgentConversationDriver
{
    Task PrepareAsync(string conversationId, string agentId, IReadOnlyList<TestState> initialStates);

    /// <summary>驱动一轮，返回本轮的输出文本。</summary>
    Task<string?> SendAsync(string conversationId, string agentId, string userMessage, CancellationToken ct);

    /// <summary>调一次 canary 函数，返回它是否被 mock 接缝接管。</summary>
    Task<bool> RunCanaryAsync(string conversationId, string agentId, CancellationToken ct);

    Task<IReadOnlyDictionary<string, string?>> ReadStatesAsync(string conversationId);

    Task<string?> ReadRoutedAgentNameAsync(string conversationId);
}
