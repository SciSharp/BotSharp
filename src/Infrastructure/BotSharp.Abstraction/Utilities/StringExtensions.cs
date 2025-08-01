using System.Text.Json;
using System.Text.RegularExpressions;

namespace BotSharp.Abstraction.Utilities;

public static class StringExtensions
{
    public static string IfNullOrEmptyAs(this string? str, string defaultValue)
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

    public static bool IsEqualTo(this string? str1, string? str2, StringComparison option = StringComparison.OrdinalIgnoreCase)
    {
        if (str1 == null) return str2 == null;

        return str1.Equals(str2, option);
    }

    public static string CleanStr(this string? str)
    {
        if (string.IsNullOrWhiteSpace(str)) return string.Empty;

        return str.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");
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

    public static string JsonArrayContent(this string text)
    {
        var m = Regex.Match(text, @"\[(.|\n|\r)*\]");
        return m.Success ? m.Value : "[]";
    }

    public static T[]? JsonArrayContent<T>(this string text)
    {
        text = JsonArrayContent(text);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            AllowTrailingCommas = true
        };

        return JsonSerializer.Deserialize<T[]>(text, options);
    }

    public static bool IsPrimitiveValue(this string value)
    {
        return int.TryParse(value, out _) ||
               long.TryParse(value, out _) ||
               double.TryParse(value, out _) ||
               float.TryParse(value, out _) ||
               bool.TryParse(value, out _) ||
               char.TryParse(value, out _) ||
               byte.TryParse(value, out _) ||
               sbyte.TryParse(value, out _) ||
               short.TryParse(value, out _) ||
               ushort.TryParse(value, out _) ||
               uint.TryParse(value, out _) ||
               ulong.TryParse(value, out _);
    }


    public static string ConvertToString<T>(this T? value, JsonSerializerOptions? jsonOptions = null)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (value is string s)
        {
            return s;
        }

        if (value is DateTime d)
        { 
            return d.ToString("o");
        }

        if (value is JsonElement elem
            && elem.ValueKind == JsonValueKind.String)
        {
            return elem.ToString();
        }

        var str = JsonSerializer.Serialize(value, jsonOptions);
        return str;
    }
}
