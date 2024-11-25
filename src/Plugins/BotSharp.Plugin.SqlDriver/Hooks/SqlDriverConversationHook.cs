
namespace BotSharp.Plugin.SqlDriver.Hooks;

public class SqlDriverConversationHook : ConversationHookBase, IConversationHook
{
    public override Task OnResponseGenerated(RoleDialogModel message)
    {
        // Render function buttons

        return base.OnResponseGenerated(message);
    }
}
