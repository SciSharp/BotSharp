namespace BotSharp.Abstraction.Utilities;

public static class StringExtensions
{
    public static string IfNullOrEmptyAs(this string str, string defaultValue)
        => string.IsNullOrEmpty(str) ? defaultValue : str;
}
