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
        var res = MessageParser.ParseTemplateMessage(root, options);
        return res;
    }

    public override void Write(Utf8JsonWriter writer, ITemplateMessage value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
