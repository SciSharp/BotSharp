using BotSharp.Core.Rules.Samples.Models;
using NRules.Fluent.Dsl;

namespace BotSharp.Core.Rules.Samples.Rules;

public class OrderValidationRule : Rule
{
    public override void Define()
    {
        Order order = default;

        When()
            .Match<Order>(() => order, o => o.Amount > 1000);

        Then()
            .Do(ctx => 
            {
                order.Status = "ReviewNeeded";
                order.Reason = "Amount too high";
            });
    }
}
