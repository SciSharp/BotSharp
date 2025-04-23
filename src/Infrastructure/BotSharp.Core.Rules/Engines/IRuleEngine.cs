using BotSharp.Abstraction.Models;

namespace BotSharp.Core.Rules.Engines;

public interface IRuleEngine
{
    Task<IEnumerable<string>> Triggered(IRuleTrigger trigger, string data, List<MessageState>? states = null);
}
