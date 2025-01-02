using BotSharp.Core.Rules.Triggers;

namespace BotSharp.Core.Rules.Engines;

public interface IRuleEngine
{
    Task Triggered(IRuleTrigger trigger, string data);
}
