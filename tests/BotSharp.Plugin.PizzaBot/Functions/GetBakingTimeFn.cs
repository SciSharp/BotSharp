using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Plugin.PizzaBot.Functions;

public class GetBakingTimeFn : IFunctionCallback
{
    public string Name => "get_cooking_remaing_time";

    public async Task<bool> Execute(RoleDialogModel message)
    {
        message.ExecutionResult = "15 minutes remaining";
        return true;
    }
}
