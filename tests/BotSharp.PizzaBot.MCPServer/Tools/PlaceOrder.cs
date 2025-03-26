using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BotSharp.PizzaBot.MCPServer.Tools;

[McpToolType]
public static class PlaceOrder
{
    [McpTool(name: "place_an_order"), Description("Place an order when user has confirmed the pizza type and quantity.")]
    public static string PlaceAnOrder(
      [Description("The pizza type."), Required] string pizza_type,
      [Description("quantity of pizza"), Required] int quantity,
      [Description("pizza unit price"),Required] double unit_price)
    {
        if (pizza_type is null)
        {
            throw new McpServerException("Missing required argument 'pizza_type'");
        }
        if (quantity <= 0)
        {
            throw new McpServerException("Missing required argument 'quantity'");
        }
        if (unit_price <= 0)
        {
            throw new McpServerException("Missing required argument 'unit_price'");
        }
 
        return "The order number is P123-01: {order_number = \"P123-01\" }";
    }
}
