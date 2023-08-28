namespace BotSharp.Abstraction.Utilities;

public static class ListExtenstion
{
    public static bool IsEmpty(this IEnumerable<string> strList)
    {
        return strList == null || !strList.Any();
    }
}
