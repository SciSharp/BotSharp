using BotSharp.Abstraction.Messaging.Enums;
using BotSharp.Abstraction.Messaging.Models.RichContent.Template;
using System.Text.Json;

namespace BotSharp.Abstraction.Messaging.JsonConverters;

public class TemplateMessageJsonConverter : JsonConverter<ITemplateMessage>
{
    public override ITemplateMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;
        var jsonText = root.GetRawText();
        JsonElement element;
        object? res = null;

        if (root.TryGetProperty("template_type", out element))
        {
            var templateType = element.GetString();
            if (templateType == TemplateTypeEnum.Button)
            {
                res = JsonSerializer.Deserialize<ButtonTemplateMessage>(jsonText, options);
            }
            else if (templateType == TemplateTypeEnum.MultiSelect)
            {
                res = JsonSerializer.Deserialize<MultiSelectTemplateMessage>(jsonText, options);
            }
            else if (templateType == TemplateTypeEnum.Coupon)
            {
                res = JsonSerializer.Deserialize<CouponTemplateMessage>(jsonText, options);
            }
            else if (templateType == TemplateTypeEnum.Product)
            {
                res = JsonSerializer.Deserialize<ProductTemplateMessage>(jsonText, options);
            }
        }

        return res as ITemplateMessage;
    }

    public override void Write(Utf8JsonWriter writer, ITemplateMessage value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
