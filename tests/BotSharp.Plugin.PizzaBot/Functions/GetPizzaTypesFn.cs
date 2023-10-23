using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Plugin.PizzaBot.Functions;

public class GetPizzaTypesFn : IFunctionCallback
{
    public string Name => "get_pizza_types";

    public async Task<bool> Execute(RoleDialogModel message)
    {
        message.Content = "Pepperoni Pizza, Cheese Pizza, Margherita Pizza";
        message.Data = new List<string>
        {
            "Pepperoni Pizza",
            "Cheese Pizza",
            "Margherita Pizza"
        };
        return true;
    }
}
