using BotSharp.Abstraction.Messaging.Enums;
using BotSharp.Abstraction.Messaging.Models.RichContent;
using BotSharp.Abstraction.Messaging.Models.RichContent.Template;
using System.Text.Json;

namespace BotSharp.Abstraction.Messaging.JsonConverters;

public class RichContentJsonConverter : JsonConverter<IRichMessage>
{
    public override IRichMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;
        var jsonText = root.GetRawText();
        JsonElement element;
        object? res = null;

        if (root.TryGetProperty("rich_type", out element))
        {
            var richType = element.GetString();
            if (richType == RichTypeEnum.ButtonTemplate)
            {
                res = JsonSerializer.Deserialize<ButtonTemplateMessage>(jsonText, options);
            }
            else if (richType == RichTypeEnum.MultiSelectTemplate)
            {
                res = JsonSerializer.Deserialize<MultiSelectTemplateMessage>(jsonText, options);
            }
            else if (richType == RichTypeEnum.QuickReply)
            {
                res = JsonSerializer.Deserialize<QuickReplyMessage>(jsonText, options);
            }
            else if (richType == RichTypeEnum.CouponTemplate)
            {
                res = JsonSerializer.Deserialize<CouponTemplateMessage>(jsonText, options);
            }
            else if (richType == RichTypeEnum.Text)
            {
                res = JsonSerializer.Deserialize<TextMessage>(jsonText, options);
            }
        }

        return res as IRichMessage;
    }

    public override void Write(Utf8JsonWriter writer, IRichMessage value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
