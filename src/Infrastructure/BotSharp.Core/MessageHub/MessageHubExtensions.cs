namespace BotSharp.Core.MessageHub;

/// <summary>
/// Raising the "working on it" line in chat.
/// </summary>
public static class MessageHubExtensions
{
    /// <summary>
    /// Announce <paramref name="indication"/> in the conversation this call is running in.
    /// <paramref name="message"/> is pushed as-is and its <c>Indication</c> overwritten, so clone it
    /// when the original is still in use by a call in flight.
    /// </summary>
    public static void PushIndication(this IServiceProvider services, RoleDialogModel message, string? indication)
    {
        var conv = services.GetRequiredService<IConversationService>();
        var hub = services.GetRequiredService<MessageHub<HubObserveData<RoleDialogModel>>>();
        hub.PushIndication(message, indication, conv.ConversationId);
    }

    /// <summary>
    /// The same, for callers already holding both -- a thread where resolving a scoped service to
    /// ask for the conversation id would be a race.
    /// </summary>
    public static void PushIndication(
        this MessageHub<HubObserveData<RoleDialogModel>> hub,
        RoleDialogModel message,
        string? indication,
        string? conversationId)
    {
        // Nothing to announce, or no one to announce it to: subscribers match on RefId.
        if (string.IsNullOrWhiteSpace(indication) || string.IsNullOrWhiteSpace(conversationId))
        {
            return;
        }

        message.Indication = indication;

        hub.Push(new()
        {
            EventName = ChatEvent.OnIndicationReceived,
            Data = message,
            RefId = conversationId
        });
    }
}
