using Microsoft.Extensions.Caching.Memory;

namespace BotSharp.Core.Infrastructures;

public static class Utilities
{
    public static string HashTextMd5(string text)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();

        var data = md5.ComputeHash(Encoding.UTF8.GetBytes(text));
        var sb = new StringBuilder();
        foreach (var c in data)
        {
            sb.Append(c.ToString("x2"));
        }
        return sb.ToString();
    }

    public static string HashTextSha256(string text)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();

        var data = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        var sb = new StringBuilder();
        foreach (var c in data)
        {
            sb.Append(c.ToString("x2"));
        }
        return sb.ToString();
    }

    public static (string, string, string) SplitAsTuple(this string str, string sep)
    {
        var splits = str.Split(sep);

        if (splits.Length == 2 || string.IsNullOrWhiteSpace(splits[2]))
        {
            return (splits[0], splits[1], "CN");
        }

        return (splits[0], splits[1], splits[2]);
    }

    /// <summary>
    /// Flush cache
    /// </summary>
    public static void ClearCache()
    {
        // Clear whole cache.
        if (new MemoryCacheAttribute(0).Cache is MemoryCache memcache)
        {
            memcache.Compact(100);
        }
    }

    public static string HideMiddleDigits(string input, bool isEmail = false)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        if (isEmail)
        {
            int atIndex = input.IndexOf('@');
            if (atIndex > 1)
            {
                string localPart = input.Substring(0, atIndex);
                if (localPart.Length > 2)
                {
                    string maskedLocalPart = $"{localPart[0]}{new string('*', localPart.Length - 2)}{localPart[^1]}";
                    return $"{maskedLocalPart}@{input.Substring(atIndex + 1)}";
                }
            }
        }
        else
        {
            if (input.Length > 6)
            {
                return $"{input.Substring(0, 3)}{new string('*', input.Length - 6)}{input.Substring(input.Length - 3)}";
            }
        }

        return input;
    }

}
