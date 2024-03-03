using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Messaging;
using BotSharp.Abstraction.Messaging.Models.RichContent;
using BotSharp.Abstraction.Messaging.Models.RichContent.Template;
using System.Linq;
using System.Text.Json;

namespace BotSharp.Plugin.PizzaBot.Functions;

public class GetPizzaTypesFn : IFunctionCallback
{
    public string Name => "get_pizza_types";

    private readonly IServiceProvider _services;
    public GetPizzaTypesFn(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var states = _services.GetRequiredService<IConversationStateService>();
        var pizzaTypes = new List<string>
        {
            "Pepperoni Pizza",
            "Cheese Pizza",
            "Margherita Pizza"
        };
        message.Content = JsonSerializer.Serialize(pizzaTypes);
        message.RichContent = new RichContent<IRichMessage>
        {
            Recipient = new Recipient
            {
                Id = states.GetConversationId()
            },
            Message = new ButtonTemplateMessage
            {
                Text = "Please select a pizza type",
                Buttons = pizzaTypes.Select(x => new ButtonElement
                {
                    Type = "text",
                    Title = x,
                    Payload = x
                }).ToArray()
            }
        };
            
        return true;
    }
}
