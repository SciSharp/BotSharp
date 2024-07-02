namespace BotSharp.Abstraction.Utilities;

public static class ListExtenstions
{
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? list)
    {
        return list == null || !list.Any();
    }
}
