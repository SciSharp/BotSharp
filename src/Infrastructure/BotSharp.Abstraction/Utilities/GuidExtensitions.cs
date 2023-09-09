namespace BotSharp.Abstraction.Utilities;

public static class GuidExtensitions
{
    public static Guid IfNullOrEmptyAsDefault(this string str)
        => string.IsNullOrEmpty(str) ? Guid.Empty : Guid.Parse(str);
}
