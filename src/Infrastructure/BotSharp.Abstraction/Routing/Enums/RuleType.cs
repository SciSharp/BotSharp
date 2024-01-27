namespace BotSharp.Abstraction.Routing.Enums;

public class RuleType
{
    /// <summary>
    /// Fallback to redirect agent
    /// </summary>
    public const string Fallback = "fallback";

    /// <summary>
    /// Redirect to other agent if data validation failed
    /// </summary>
    public const string DataValidation = "data-validation";

    /// <summary>
    /// The planning approach name for next step
    /// </summary>
    public const string Planner = "planner";
}
