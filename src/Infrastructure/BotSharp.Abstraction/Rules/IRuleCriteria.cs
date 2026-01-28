using BotSharp.Abstraction.Rules.Models;

namespace BotSharp.Abstraction.Rules;

public interface IRuleCriteria
{
    string Provider { get; }

    Task<RuleCriteriaResult> ValidateAsync(Agent agent, IRuleTrigger trigger, RuleCriteriaContext context)
        => Task.FromResult(new RuleCriteriaResult());
}
