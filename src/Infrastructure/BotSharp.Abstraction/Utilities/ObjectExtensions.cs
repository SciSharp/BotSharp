using BotSharp.Abstraction.Options;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace BotSharp.Abstraction.Utilities;

public static class ObjectExtensions
{
    private static readonly JsonSerializerOptions _defaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowTrailingCommas = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public static T? DeepClone<T>(this T? inputObj, Action<T>? modifier = null, BotSharpOptions? options = null) where T : class
    {
        if (inputObj == null)
        {
            return null;
        }

        try
        {
            var json = JsonSerializer.Serialize(inputObj, options?.JsonSerializerOptions ?? _defaultJsonOptions);
            var outputObj = JsonSerializer.Deserialize<T>(json, options?.JsonSerializerOptions ?? _defaultJsonOptions);
            if (modifier != null && outputObj != null)
            {
                modifier(outputObj);
            }

            return outputObj;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DeepClone Error in {nameof(DeepClone)} for {typeof(T).Name}: {ex}");
            return null;
        }            
    }

    public static TOutput? DeepClone<TInput, TOutput>(this TInput? inputObj, Action<TOutput>? modifier = null, BotSharpOptions? options = null)
        where TInput : class
        where TOutput : class
    {
        if (inputObj == null)
        {
            return null;
        }

        try
        {
            var json = JsonSerializer.Serialize(inputObj, options?.JsonSerializerOptions ?? _defaultJsonOptions);
            var outputObj = JsonSerializer.Deserialize<TOutput>(json, options?.JsonSerializerOptions ?? _defaultJsonOptions);
            if (modifier != null && outputObj != null)
            {
                modifier(outputObj);
            }

            return outputObj;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DeepClone Error in {nameof(DeepClone)} for {typeof(TInput).Name} and {typeof(TOutput).Name}: {ex}");
            return null;
        }
    }

    public static T? TryGetValueOrDefault<T>(this IDictionary<string, object?> dict, string key, T? defaultValue = default, JsonSerializerOptions? jsonOptions = null)
    {
        return dict.TryGetValue<T>(key, out var value, jsonOptions)
                ? value!
                : defaultValue;
    }

    public static bool TryGetValue<T>(this IDictionary<string, object?> dict, string key, out T? result, JsonSerializerOptions? jsonOptions = null)
    {
        result = default;

        if (!dict.TryGetValue(key, out var value) || value is null)
        {
            return false;
        }

        if (value is T t)
        {
            result = t;
            return true;
        }

        if (value is JsonElement je)
        {
            try
            {
                result = je.Deserialize<T>(jsonOptions);
                return true;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }
}
