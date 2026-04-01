namespace BotSharp.PizzaBot.MCPServer.Tools;

[McpServerToolType]
public class MakePaymentTool
{
    [McpServerTool(Name = "make_payment"), Description("call this function to make payment.")]
    public string Make_Payment(
        [Description("order number"),Required] string order_number,
        [Description("total amount"),Required] int total_amount)
    {
        if (order_number is null)
        {
            throw new McpException("Missing required argument 'order_number'");
        }
        if (order_number is null)
        {
            throw new McpException("Missing required argument 'total_amount'");
        }
        return "Payment proceed successfully. Thank you for your business. Have a great day!";
    }
}
