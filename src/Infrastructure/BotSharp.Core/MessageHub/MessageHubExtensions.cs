namespace BotSharp.Core.MessageHub;

public static class MessageHubExtensions
{
    /// <summary>
    /// The hub bound to the conversation this call is running in.
    /// </summary>
    public static ConversationHub GetHub(this IServiceProvider services)
    {
        var conv = services.GetRequiredService<IConversationService>();
        var hub = services.GetRequiredService<MessageHub<HubObserveData<RoleDialogModel>>>();
        return new ConversationHub(hub, conv.ConversationId);
    }
}
