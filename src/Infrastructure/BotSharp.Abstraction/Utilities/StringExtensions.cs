using System.Text.Json;
using System.Text.RegularExpressions;

namespace BotSharp.Abstraction.Utilities;

public static class StringExtensions
{
    public static string IfNullOrEmptyAs(this string str, string defaultValue)
        => string.IsNullOrEmpty(str) ? defaultValue : str;

    public static string SubstringMax(this string str, int maxLength)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        if (str.Length > maxLength)
            return str.Substring(0, maxLength);
        else
            return str;
    }

    public static string[] SplitByNewLine(this string input)
    {
        if (input == null)
        {
            return new string[0];
        }
        return input.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
    }

    public static string RemoveNewLine(this string input)
    {
        if (input == null)
        {
            return null;
        }
        return input.Replace("\r", " ").Replace("\n", " ").Trim();
    }

    public static bool IsEqualTo(this string str1, string str2, StringComparison option = StringComparison.OrdinalIgnoreCase)
    {
        return str1.Equals(str2, option);
    }

    public static string JsonContent(this string text)
    {
        var m = Regex.Match(text, @"\{(?:[^{}]|(?<open>\{)|(?<-open>\}))+(?(open)(?!))\}");
        return m.Success ? m.Value : "{}";
    }

    public static T? JsonContent<T>(this string text)
    {
        text = JsonContent(text);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            AllowTrailingCommas = true
        };

        return JsonSerializer.Deserialize<T>(text, options);
    }
}
