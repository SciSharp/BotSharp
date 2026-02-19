using BotSharp.Abstraction.Rules.Models;
using System.Text.Json;

namespace BotSharp.Abstraction.Rules;

public interface IRuleCriteria
{
    string Provider { get; }

    JsonDocument DefaultConfig => JsonDocument.Parse("{}");

    Task<RuleCriteriaResult> ValidateAsync(Agent agent, IRuleTrigger trigger, RuleCriteriaContext context)
        => Task.FromResult(new RuleCriteriaResult());
}
