using BotSharp.Abstraction.Options;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BotSharp.Abstraction.Utilities;

public static partial class StringExtensions
{
    public static string? IfNullOrEmptyAs(this string? str, string? defaultValue)
        => string.IsNullOrEmpty(str) ? defaultValue : str;

    public static string SubstringMax(this string str, int maxLength)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        if (str.Length > maxLength)
        {
            return str.Substring(0, maxLength);
        }
        else
        {
            return str;
        }
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
        if (str1 == null)
        {
            return str2 == null;
        }

        return str1.Equals(str2, option);
    }

    public static string CleanStr(this string? str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return string.Empty;
        }

        return str.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");
    }

    /// <summary>
    /// Normalizes function name by removing namespace/agent prefixes.
    /// LLM sometimes returns function names like "AgentName.FunctionName" or "Namespace.FunctionName".
    /// This method extracts the actual function name. Returns original input if normalization would
    /// yield empty/whitespace (e.g. "Agent." or "Agent/ ") to avoid downstream invalid function lookup.
    /// </summary>
    /// <param name="functionName">The raw function name from LLM response</param>
    /// <returns>The normalized function name, or original if normalized would be empty/whitespace</returns>
    public static string? NormalizeFunctionName(this string? functionName)
    {
        functionName = functionName?.Trim();
        if (string.IsNullOrEmpty(functionName))
        {
            return functionName;
        }

        foreach (var separator in new[] { '.', '/' })
        {
            if (!functionName.Contains(separator))
            {
                continue;
            }

            var segments = functionName.Split(separator);
            var normalized = segments.LastOrDefault()?.Trim();

            if (string.IsNullOrWhiteSpace(normalized))
            {
                return functionName;
            }

            Console.WriteLine($"NormalizeFunctionName: {functionName} -> {normalized}");
            return normalized;
        }

        return functionName;
    }

    public static string CleanJsonStr(this string? str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return string.Empty;
        }

        return str.Replace("```json", string.Empty).Replace("```", string.Empty).Trim();
    }

    public static T? Json<T>(this string text)
    {
        return JsonSerializer.Deserialize<T>(text, BotSharpOptions.defaultJsonOptions);
    }

    public static string JsonContent(this string text)
    {
        var m = Regex.Match(text, @"\{(?:[^{}]|(?<open>\{)|(?<-open>\}))+(?(open)(?!))\}");
        return m.Success ? m.Value : "{}";
    }

    public static T? JsonContent<T>(this string text)
    {
        text = JsonContent(text);

        return JsonSerializer.Deserialize<T>(text, BotSharpOptions.defaultJsonOptions);
    }

    public static string JsonArrayContent(this string text)
    {
        var m = Regex.Match(text, @"\[(.|\n|\r)*\]");
        return m.Success ? m.Value : "[]";
    }

    public static T[]? JsonArrayContent<T>(this string text)
    {
        text = JsonArrayContent(text);

        return JsonSerializer.Deserialize<T[]>(text, BotSharpOptions.defaultJsonOptions);
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
