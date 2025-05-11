namespace BotSharp.Abstraction.Rules;

public interface IRuleEngine
{
    Task<IEnumerable<string>> Triggered(IRuleTrigger trigger, string data, List<MessageState>? states = null);
}
