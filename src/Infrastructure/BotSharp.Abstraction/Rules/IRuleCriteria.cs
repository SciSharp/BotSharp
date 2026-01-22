namespace BotSharp.Abstraction.Rules;

public interface IRuleCriteria
{
    string Provider { get; }

    Task<bool> ValidateAsync(Agent agent, IRuleTrigger trigger, CriteriaExecuteOptions options)
        => Task.FromResult(false);
}
