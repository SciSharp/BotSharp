using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Plugin.PizzaBot.Functions;

public class GetDeliveryTimeFn : IFunctionCallback
{
    public string Name => "get_delivery_time";

    public async Task<bool> Execute(RoleDialogModel message)
    {
        message.ExecutionResult = "15 minutes remaining";
        return true;
    }
}
