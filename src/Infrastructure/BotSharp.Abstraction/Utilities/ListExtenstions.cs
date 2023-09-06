namespace BotSharp.Abstraction.Utilities;

public static class ListExtenstions
{
    public static bool IsNullOrEmpty<T>(this IEnumerable<T> strList)
    {
        return strList == null || !strList.Any();
    }
}
