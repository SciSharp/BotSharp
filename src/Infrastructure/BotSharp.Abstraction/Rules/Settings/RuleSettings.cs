namespace BotSharp.Abstraction.Rules.Settings;

public class RuleSettings
{
    /// <summary>
    /// How many triggered rules may run at the same time. Overridden per call by
    /// <c>RuleTriggerOptions.MaxConcurrency</c>. Null falls back to the built-in default.
    /// </summary>
    public int? MaxConcurrency { get; set; }
}
