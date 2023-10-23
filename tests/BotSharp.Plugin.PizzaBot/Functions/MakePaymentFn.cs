using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Plugin.PizzaBot.Functions;

public class MakePaymentFn : IFunctionCallback
{
    public string Name => "make_payment";

    public async Task<bool> Execute(RoleDialogModel message)
    {
        message.Content = "Payment proceed successfully. Thank you for your business. Have a great day!";
        message.Data = new
        {
            Transaction = Guid.NewGuid().ToString(),
            Status = "Success"
        };
        return true;
    }
}
