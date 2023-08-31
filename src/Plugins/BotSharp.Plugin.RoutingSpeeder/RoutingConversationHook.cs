using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using System.Threading.Tasks;

namespace BotSharp.Plugin.RoutingSpeeder;

public class RoutingConversationHook: ConversationHookBase
{
    public override async Task BeforeCompletion(RoleDialogModel message)
    {
        // Utilize local discriminative model to predict intent
        message.Content = "response content";
        message.StopCompletion = true;
    }
}
