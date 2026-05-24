namespace BotSharp.Plugin.CodeAct.Security;

public static class CodeActImpact
{
    public const string Read = "read";
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";

    public static bool IsHighImpact(string? impact)
    {
        return string.Equals(impact, High, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsReadOnly(string? impact)
    {
        return string.Equals(impact, Read, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(impact, Low, StringComparison.OrdinalIgnoreCase);
    }
}
