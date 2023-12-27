using Microsoft.Extensions.Caching.Memory;

namespace BotSharp.Core.Infrastructures;

public static class Utilities
{
    public static string HashText(string password, string salt)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();

        var data = md5.ComputeHash(Encoding.UTF8.GetBytes(password + salt));
        var sb = new StringBuilder();
        foreach (var c in data)
        {
            sb.Append(c.ToString("x2"));
        }
        return sb.ToString();
    }

    public static (string, string) SplitAsTuple(this string str, string sep)
    {
        var splits = str.Split(sep);
        return (splits[0], splits[1]);
    }

    public static void ClearCache()
    {
        // Clear whole cache.
        if (new MemoryCacheAttribute(0).Cache is MemoryCache memcache)
        {
            memcache.Compact(100);
        }
    }
}
