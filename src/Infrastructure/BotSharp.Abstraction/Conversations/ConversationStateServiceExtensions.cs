using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Options;
using Newtonsoft.Json;
using System.Globalization;

namespace BotSharp.Abstraction.Conversations;

public static class ConversationStateServiceExtensions
{
    public static bool Equal(this IConversationStateService states, string name, string value)
    {
        return states.GetState(name) == value;
    }

    public static bool NotEqual(this IConversationStateService states, string name, string value)
    {
        return !states.Equal(name, value);
    }

    public static bool IsTrue(this IConversationStateService states, string name)
    {
        var value = states.GetState(name, "false");
        return bool.TryParse(value, out var result) ? result : false;
    }

    public static bool IsFalse(this IConversationStateService states, string name)
    {
        return !states.IsTrue(name);
    }

    public static bool IsNullOrEmpty(this IConversationStateService states, string name)
    {
        return string.IsNullOrEmpty(states.GetState(name));
    }

    public static bool IsNotNullOrEmpty(this IConversationStateService states, string name)
    {
        return !IsNullOrEmpty(states, name);
    }

    /// <summary>
    /// Get a state value and convert it to the target type.
    /// Primitive/decimal/string/DateTime/enum are parsed with invariant culture, other types are deserialized from json.
    /// </summary>
    public static T GetState<T>(this IConversationStateService states, string name, T defaultValue = default!, bool useNewtonsoftJson = true)
    {
        try
        {
            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            var isBasicType = targetType.IsPrimitive || targetType == typeof(decimal) || targetType == typeof(string);
            var value = states.GetState(name, isBasicType ? defaultValue?.ToString() ?? "" : "");
            if (string.IsNullOrEmpty(value)) return defaultValue;

            if (isBasicType)
            {
                return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            else if (targetType == typeof(DateTime))
            {
                return (T)(object)DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            }
            else if (targetType.IsEnum)
            {
                return (T)Enum.Parse(targetType, value, ignoreCase: true);
            }
            else
            {
                var result = useNewtonsoftJson
                    ? JsonConvert.DeserializeObject<T>(value)
                    : System.Text.Json.JsonSerializer.Deserialize<T>(value, BotSharpOptions.defaultJsonOptions);
                return result is null ? defaultValue : result;
            }
        }
        catch
        {
            // fall back to the default value when the state cannot be converted
        }

        return defaultValue;
    }

    public static bool IsPhoneChannel(this IConversationStateService states)
    {
        return states.GetState(StateConst.CHANNEL) == ConversationChannel.Phone;
    }

    public static bool IsEmailChannel(this IConversationStateService states)
    {
        return states.GetState(StateConst.CHANNEL) == ConversationChannel.Email;
    }

    public static bool IsOpenAPIChannel(this IConversationStateService states)
    {
        return states.GetState(StateConst.CHANNEL) == ConversationChannel.OpenAPI;
    }
}
