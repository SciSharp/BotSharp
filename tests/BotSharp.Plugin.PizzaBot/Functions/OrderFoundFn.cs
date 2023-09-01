using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Plugin.PizzaBot.Functions;

public class OrderFoundFn : IFunctionCallback
{
    public string Name => "order_found";

    public async Task<bool> Execute(RoleDialogModel message)
    {
        message.ExecutionResult = "The order number is P123-01";
        return true;
    }
}
