namespace BotSharp.Abstraction.Routing.Enums;

public static class InvokeSource
{
    /// <summary>
    /// Invoke manually
    /// </summary>
    public const string Manual = "manual";

    /// <summary>
    /// Invoke by LLM directly
    /// </summary>
    public const string Llm = "llm";

    /// <summary>
    /// Invoke by agent routing
    /// </summary>
    public const string Routing = "routing";
}
