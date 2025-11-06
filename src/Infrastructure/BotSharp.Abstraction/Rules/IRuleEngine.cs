namespace BotSharp.Abstraction.Rules;

public interface IRuleEngine
{
    /// <summary>
    /// Trigger the rule that is subscribed by agents.
    /// </summary>
    /// <param name="trigger"></param>
    /// <param name="text"></param>
    /// <param name="states"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    Task<IEnumerable<string>> Trigger(IRuleTrigger trigger, string text, IEnumerable<MessageState>? states = null, RuleTriggerOptions? options = null)
        => throw new NotImplementedException();
}
