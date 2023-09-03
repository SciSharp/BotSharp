namespace BotSharp.Abstraction.Utilities;

public static class ListExtenstion
{
    public static bool IsEmpty<T>(this IEnumerable<T> strList)
    {
        return strList == null || !strList.Any();
    }
}
