using BotSharp.Core.Messaging;
using System.Text.Json;

namespace BotSharp.Abstraction.Messaging.JsonConverters;

public class TemplateMessageJsonConverter : JsonConverter<ITemplateMessage>
{
    public override ITemplateMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;
        var jsonText = root.GetRawText();
        ITemplateMessage? res = null;

        var parser = new MessageParser();
        if (root.TryGetProperty("template_type", out JsonElement element))
        {
            var templateType = element.GetString();
            res = parser.ParseTemplateMessage(templateType, jsonText, options);
        }

        return res;
    }

    public override void Write(Utf8JsonWriter writer, ITemplateMessage value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
