namespace BotSharp.Abstraction.Rules.Options;

public class RuleMethodOptions
{
    public Func<Agent, Task>? Func { get; set; }
}
