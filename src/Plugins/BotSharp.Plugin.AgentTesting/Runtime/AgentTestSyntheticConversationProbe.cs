using BotSharp.Abstraction.Conversations;

namespace BotSharp.Plugin.AgentTesting.Runtime;

/// <summary>
/// Tells BotSharp which conversations belong to a test run, so its per-user volume limits can stand
/// aside for them.
///
/// Answers from <see cref="IAgentTestRunRegistry"/> -- the same registry that decides whether a
/// function call is intercepted -- rather than from the conversation's "test-set" tag. The tag is
/// written only once the conversation row exists, which is after the first message has already passed
/// through the rate limit hook, so a tag-based answer would still block the first turn of every case.
/// The registry entry is created before the conversation is opened at all.
///
/// It is also the same source of truth as the mock seam, which matters: a conversation the harness
/// mocks tools for and a conversation the harness is exempt from rate limiting are, by construction,
/// the same set. Two independent notions of "is this a test" could disagree, and the disagreement
/// would show up as either real traffic escaping the limits or tests being blocked by them.
/// </summary>
public class AgentTestSyntheticConversationProbe : ISyntheticConversationProbe
{
    private readonly IAgentTestRunRegistry _registry;

    public AgentTestSyntheticConversationProbe(IAgentTestRunRegistry registry)
    {
        _registry = registry;
    }

    public bool IsSynthetic(string conversationId)
        => !string.IsNullOrEmpty(conversationId) && _registry.TryGet(conversationId) != null;
}
