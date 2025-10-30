namespace BotSharp.Abstraction.Rules;

public interface IRuleEngine
{
    Task<IEnumerable<string>> Trigger(IRuleTrigger trigger, string text, RuleTriggerOptions? options = null)
        => throw new NotImplementedException();
}
