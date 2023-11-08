using System.Text.Json;

namespace BotSharp.Abstraction.Messaging.Models;

public class RichContentJsonConverter : JsonConverter<IMessageTemplate>
{
    public override IMessageTemplate? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, IMessageTemplate value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
