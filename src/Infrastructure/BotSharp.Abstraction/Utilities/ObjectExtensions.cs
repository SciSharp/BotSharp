using BotSharp.Abstraction.Options;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace BotSharp.Abstraction.Utilities;

public static class ObjectExtensions
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowTrailingCommas = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public static T? DeepClone<T>(this T? obj, Action<T>? modifier = null, BotSharpOptions? options = null) where T : class
    {
        if (obj == null)
        {
            return null;
        }

        try
        {
            var json = JsonSerializer.Serialize(obj, options?.JsonSerializerOptions ?? DefaultJsonOptions);
            var newObj = JsonSerializer.Deserialize<T>(json, options?.JsonSerializerOptions ?? DefaultJsonOptions);
            if (modifier != null && newObj != null)
            {
                modifier(newObj);
            }

            return newObj;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DeepClone Error in {nameof(DeepClone)}: {ex}");
            return null;
        }            
    }
}
