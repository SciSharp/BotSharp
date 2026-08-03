using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.Json;
using BotSharp.Abstraction.Options;

namespace BotSharp.Abstraction.Utilities;

public static class JsonExtensions
{
    public static string FormatJson(this string? json, Formatting format = Formatting.Indented)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return "{}";
        }

        try
        {
            var parsedJson = JObject.Parse(json);
            foreach (var item in parsedJson)
            {
                try
                {
                    var key = item.Key;
                    var value = parsedJson[key].ToString();
                    var parsedValue = JObject.Parse(value);
                    parsedJson[key] = parsedValue;
                }
                catch { continue; }
            }

            var jsonSettings = new JsonSerializerSettings
            {
                Formatting = format
            };
            return JsonConvert.SerializeObject(parsedJson, jsonSettings);
        }
        catch
        {
            return json;
        }
    }

    /// <summary>
    /// Convert any object into a <see cref="JsonDocument"/>. A string input is treated as raw json first,
    /// and only serialized as a json string value when it cannot be parsed.
    /// </summary>
    /// <returns>Null when the input is null or cannot be represented as json. The caller owns the returned document and should dispose it.</returns>
    public static JsonDocument? ToJsonDoc(this object? obj, JsonSerializerOptions? jsonOptions = null)
    {
        var json = obj.ToJsonString(jsonOptions);
        if (json == null)
        {
            return null;
        }

        try
        {
            return JsonDocument.Parse(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Convert any object into a <see cref="JsonElement"/>. The returned element is cloned,
    /// so it stays valid after the underlying document is disposed.
    /// </summary>
    /// <returns>Null when the input is null or cannot be represented as json.</returns>
    public static JsonElement? ToJsonElement(this object? obj, JsonSerializerOptions? jsonOptions = null)
    {
        using var doc = obj.ToJsonDoc(jsonOptions);
        return doc?.RootElement.Clone();
    }

    private static string? ToJsonString(this object? obj, JsonSerializerOptions? jsonOptions)
    {
        if (obj == null)
        {
            return null;
        }

        // A string is most likely already json, e.g. a serialized payload or a raw llm output.
        if (obj is string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return null;
            }

            try
            {
                using var parsed = JsonDocument.Parse(str);
                return str;
            }
            catch
            {
                // Not json, fall through and serialize it as a json string value.
            }
        }

        try
        {
            return System.Text.Json.JsonSerializer.Serialize(obj, jsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
