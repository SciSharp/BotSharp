namespace BotSharp.Abstraction.Rules.Options;

public class RuleActionOptions
{
    /// <summary>
    /// Rule action provider
    /// </summary>
    public string Provider { get; set; } = "botsharp-rule";

    /// <summary>
    /// Delay message options
    /// </summary>
    public RuleDelayMessageOptions? DelayMessage { get; set; }
}
