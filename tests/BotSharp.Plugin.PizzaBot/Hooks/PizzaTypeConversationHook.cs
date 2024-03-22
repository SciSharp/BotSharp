using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Plugin.PizzaBot.Hooks;

public class PizzaTypeConversationHook : ConversationHookBase
{
    public override async Task OnPostbackMessageReceived(RoleDialogModel message, PostbackMessageModel replyMsg)
    {
        if (replyMsg.FunctionName == "get_pizza_types")
        {
            // message.StopCompletion = true;
        }
        return;
    }

    public override Task OnTaskCompleted(RoleDialogModel message)
    {
        return base.OnTaskCompleted(message);
    }
}
