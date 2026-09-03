using System.Threading;

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
    /// <param name="cancellationToken">Stops dispatching further rules and cancels the pause between them.</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    Task<IEnumerable<string>> Triggered(IRuleTrigger trigger, string text, IEnumerable<MessageState>? states = null, RuleTriggerOptions? options = null, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
