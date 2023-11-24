using System.Text.Json;

namespace BotSharp.Abstraction.Messaging;

public class RichContentJsonConverter : JsonConverter<IRichMessage>
{
    public override IRichMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, IRichMessage value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
