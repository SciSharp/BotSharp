namespace BotSharp.Abstraction.Rules.Options;

public class RuleActionOptions
{
    /// <summary>
    /// Rule action provider
    /// </summary>
    public string Provider { get; set; } = "botsharp-rule";

    /// <summary>
    /// Event message options
    /// </summary>
    public RuleEventMessageOptions? EventMessage { get; set; }

    /// <summary>
    /// Custom method options
    /// </summary>
    public RuleMethodOptions? Method { get; set; }
}
