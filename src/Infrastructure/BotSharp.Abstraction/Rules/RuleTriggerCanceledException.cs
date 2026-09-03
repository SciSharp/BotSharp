using System.Threading;

namespace BotSharp.Abstraction.Rules;

/// <summary>
/// Thrown when a rule trigger is cancelled part way through. Rules that already started a
/// conversation cannot be undone, so their ids are carried on the exception and the caller
/// can still act on them (or ignore them) while seeing the run as cancelled.
/// </summary>
public class RuleTriggerCanceledException : OperationCanceledException
{
    /// <summary>
    /// Conversations that were created before the run was cancelled. Never null.
    /// </summary>
    public IReadOnlyList<string> ConversationIds { get; }

    public RuleTriggerCanceledException(
        IReadOnlyList<string> conversationIds,
        CancellationToken cancellationToken,
        Exception? innerException = null)
        : base($"Rule trigger was cancelled after starting {conversationIds.Count} conversation(s).", innerException, cancellationToken)
    {
        ConversationIds = conversationIds;
    }
}
