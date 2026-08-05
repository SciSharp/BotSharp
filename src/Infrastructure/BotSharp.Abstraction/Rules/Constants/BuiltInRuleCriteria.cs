namespace BotSharp.Abstraction.Rules.Constants;

/// <summary>
/// Built-in rule criteria types. Each value maps to an
/// <see cref="IRuleCriteriaEvaluator.Type"/> registered in DI.
/// Plugins may introduce additional string types.
/// </summary>
public static class BuiltInRuleCriteria
{
    /// <summary>
    /// Evaluate a code script (e.g. Python) that returns a boolean result.
    /// </summary>
    public const string Code = "code";

    /// <summary>
    /// Ask an LLM whether the rule applies to the request.
    /// </summary>
    public const string Llm = "llm";
}
