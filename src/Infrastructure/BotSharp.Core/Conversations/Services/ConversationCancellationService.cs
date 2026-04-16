using System.Collections.Concurrent;

namespace BotSharp.Core.Conversations.Services;

public class ConversationCancellationService : IConversationCancellationService
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokenSources = new();
    private readonly ILogger _logger;

    public ConversationCancellationService(
        ILogger<ConversationCancellationService> logger)
    {
        _logger = logger;
    }

    public CancellationToken RegisterConversation(string conversationId)
    {
        // Cancel any existing streaming for this conversation
        if (_cancellationTokenSources.TryRemove(conversationId, out var existingCts))
        {
            existingCts.Cancel();
            existingCts.Dispose();
            _logger.LogWarning("Cancelled existing streaming session for conversation {ConversationId}", conversationId);
        }

        var cts = new CancellationTokenSource();
        _cancellationTokenSources[conversationId] = cts;
        _logger.LogInformation("Registered streaming cancellation for conversation {ConversationId}", conversationId);
        return cts.Token;
    }

    public bool CancelStreaming(string conversationId)
    {
        if (_cancellationTokenSources.TryGetValue(conversationId, out var cts))
        {
            cts.Cancel();
            _logger.LogInformation("Streaming cancelled for conversation {ConversationId}", conversationId);
            return true;
        }

        _logger.LogWarning("No active streaming found for conversation {ConversationId}", conversationId);
        return false;
    }

    public void UnregisterConversation(string conversationId)
    {
        if (_cancellationTokenSources.TryRemove(conversationId, out var cts))
        {
            cts.Dispose();
            _logger.LogDebug("Unregistered streaming cancellation for conversation {ConversationId}", conversationId);
        }
    }

    public CancellationToken GetToken(string conversationId)
    {
        if (_cancellationTokenSources.TryGetValue(conversationId, out var cts))
        {
            return cts.Token;
        }

        return CancellationToken.None;
    }
}
