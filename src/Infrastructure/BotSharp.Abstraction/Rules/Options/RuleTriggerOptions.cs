namespace BotSharp.Abstraction.Rules.Options;

public class RuleTriggerOptions
{
    /// <summary>
    /// Criteria options for validating whether the rule should be triggered
    /// </summary>
    public RuleCriteriaOptions? Criteria { get; set; }
}
