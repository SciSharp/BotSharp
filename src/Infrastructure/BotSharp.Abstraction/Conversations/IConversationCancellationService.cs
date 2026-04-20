using System.Threading;

namespace BotSharp.Abstraction.Conversations;

/// <summary>
/// Service to manage cancellation tokens for streaming chat completions.
/// Allows stopping an active streaming response by conversation ID.
/// </summary>
public interface IConversationCancellationService
{
    /// <summary>
    /// Register a new cancellation token source for the given conversation.
    /// Returns the CancellationToken to be used in streaming loops.
    /// </summary>
    CancellationToken RegisterConversation(string conversationId);

    /// <summary>
    /// Cancel an active streaming operation for the given conversation.
    /// </summary>
    /// <returns>True if the conversation was found and cancelled, false otherwise.</returns>
    bool CancelStreaming(string conversationId);

    /// <summary>
    /// Remove the cancellation token source for the given conversation.
    /// Should be called when streaming completes (either normally or via cancellation).
    /// </summary>
    void UnregisterConversation(string conversationId);

    /// <summary>
    /// Get the cancellation token for the given conversation if one is registered.
    /// </summary>
    CancellationToken GetToken(string conversationId);
}
