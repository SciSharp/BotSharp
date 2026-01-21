namespace BotSharp.Abstraction.Rules;

public interface IRuleCriteria
{
    string Provider { get; }

    Task<bool> ExecuteCriteriaAsync(Agent agent, string triggerName, CriteriaExecuteOptions options)
        => throw new NotImplementedException();
}
