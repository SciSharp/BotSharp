using BotSharp.Core.Messaging;
using System.Text.Json;

namespace BotSharp.Abstraction.Messaging.JsonConverters;

public class RichContentJsonConverter : JsonConverter<IRichMessage>
{
    public override IRichMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;
        var jsonText = root.GetRawText();
        IRichMessage? res = null;

        var parser = new MessageParser();
        if (root.TryGetProperty("rich_type", out JsonElement element))
        {
            var richType = element.GetString();
            res = parser.ParseRichMessage(richType, jsonText, root, options);
        }

        return res;
    }

    public override void Write(Utf8JsonWriter writer, IRichMessage value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}