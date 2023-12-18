using System.Text.Json.Serialization.Metadata;

namespace BotSharp.Plugin.MetaMessenger.Services;

public static class ConditionalSerialization
{
    /// <summary>
    /// https://devblogs.microsoft.com/dotnet/system-text-json-in-dotnet-7/#example-conditional-serialization
    /// </summary>
    /// <param name="typeInfo"></param>
    public static void IgnoreRichType(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type.GetInterface(nameof(IRichMessage)) == null)
            return;

        foreach (JsonPropertyInfo propertyInfo in typeInfo.Properties)
        {
            if (propertyInfo.Name == "rich_type")
            {
                propertyInfo.ShouldSerialize = static (obj, value) => false;
            }
        }
    }
}
