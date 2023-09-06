using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Plugin.PizzaBot.Functions;

public class PlaceOrderFn : IFunctionCallback
{
    public string Name => "place_an_order";

    public async Task<bool> Execute(RoleDialogModel message)
    {
        message.ExecutionResult = "The order number is P123-01";
        return true;
    }
}
