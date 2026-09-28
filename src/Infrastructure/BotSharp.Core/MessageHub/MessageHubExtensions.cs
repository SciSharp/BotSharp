using BotSharp.Abstraction.Infrastructures.Enums;

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

        // Resolved now, not at push time: a hub may be kept and pushed from a transport's own thread.
        var localizer = services.GetService<IIndicationLocalizer>();
        var language = conv.States.GetState(StateConst.LANGUAGE, LanguageType.ENGLISH);
        Func<string, string>? localize = localizer == null ? null : text => localizer.Localize(text, language);

        return new ConversationHub(hub, conv.ConversationId, localize);
    }
}
