namespace BotSharp.Core.Rules.Samples.Models;

public class Order
{
    public string Id { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; }
    public bool IsValid { get; set; } = true;
    public string Reason { get; set; }
}
