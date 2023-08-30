using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Plugin.PizzaBot.Functions;

public class MakeOrderFn : IFunctionCallback
{
    public string Name => "make_order";

    public async Task<bool> Execute(RoleDialogModel message)
    {
        message.ExecutionResult = "The order number is P123-01";
        return true;
    }
}
