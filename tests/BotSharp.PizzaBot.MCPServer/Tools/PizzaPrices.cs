using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Text.Json;

namespace BotSharp.PizzaBot.MCPServer.Tools;

[McpServerToolType]
public static class PizzaPrices
{
    [McpServerTool(Name = "get_pizza_prices"), Description("call this function to get pizza unit price.")]
    public static string GetPizzaPrices(
       [Description("The pizza type."), Required] string pizza_type,
       [Description("quantity of pizza"), Required] int quantity)
    {
        if (pizza_type is null)
        {
            throw new McpException("Missing required argument 'pizza_type'");
        }
        if (quantity <= 0)
        {
            throw new McpException("Missing required argument 'quantity'");
        }
        double unit_price = 0;
        if (pizza_type.ToString() == "Pepperoni Pizza")
        {
            unit_price = 3.2 * (int)quantity;
        }
        else if (pizza_type.ToString() == "Cheese Pizza")
        {
            unit_price = 3.5 * (int)quantity; ;
        }
        else if (pizza_type.ToString() == "Margherita Pizza")
        {
            unit_price = 3.8 * (int)quantity; ;
        }

        dynamic message = new ExpandoObject();
        message.unit_price = unit_price;
        var jso = new JsonSerializerOptions() { WriteIndented = true };
        var jsonMessage = JsonSerializer.Serialize(message, jso);

        return jsonMessage;
    }
}
