using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Infrastructures.Enums;

namespace BotSharp.Plugin.PizzaBot.Functions;

public class PlaceOrderFn : IFunctionCallback
{
    public string Name => "place_order";

    private readonly IServiceProvider _service;
    public PlaceOrderFn(IServiceProvider service)
    {
        _service = service;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        message.Content = "The order number is P123-01";
        var state = _service.GetRequiredService<IConversationStateService>();
        state.SetState("order_number", "P123-01");

        // Set the next action agent to Payment
        state.SetState(StateConst.EXPECTED_ACTION_AGENT, "Payment", activeRounds: 2);

        return true;
    }
}
