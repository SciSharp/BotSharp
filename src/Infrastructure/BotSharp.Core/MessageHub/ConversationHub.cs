namespace BotSharp.Core.MessageHub;

/// <summary>
/// Pushing events into one conversation. The id is captured when the hub is taken, on the thread
/// that asked for it -- a reporter running on a transport's own thread can keep this and push
/// without resolving a scoped service again. Don't hold one past the conversation it was taken for.
/// </summary>
public sealed class ConversationHub
{
    private readonly MessageHub<HubObserveData<RoleDialogModel>> _hub;

    internal ConversationHub(MessageHub<HubObserveData<RoleDialogModel>> hub, string? conversationId)
    {
        _hub = hub;
        ConversationId = conversationId;
    }

    /// <summary>Where every push goes; null when taken outside a conversation.</summary>
    public string? ConversationId { get; }

    /// <summary>
    /// Raise the "working on it" line in chat. <paramref name="message"/> is pushed as-is and its
    /// <c>Indication</c> overwritten, so clone it when the original is still in use by a call in flight.
    /// </summary>
    public void PushIndication(RoleDialogModel message, string? indication)
    {
        // Nothing to announce, or no one to announce it to: subscribers match on RefId.
        if (string.IsNullOrWhiteSpace(indication) || string.IsNullOrWhiteSpace(ConversationId))
        {
            return;
        }

        message.Indication = indication;

        _hub.Push(new()
        {
            EventName = ChatEvent.OnIndicationReceived,
            Data = message,
            RefId = ConversationId
        });
    }
}
