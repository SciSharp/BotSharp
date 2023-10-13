using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Plugin.PizzaBot.Functions;

public class PlaceOrderFn : IFunctionCallback
{
    public string Name => "place_an_order";

    private readonly IServiceProvider _service;
    public PlaceOrderFn(IServiceProvider service)
    {
        _service = service;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        message.ExecutionResult = "The order number is P123-01";
        var state = _service.GetRequiredService<IConversationStateService>();
        state.SetState("order_number", "P123-01");

        return true;
    }
}
