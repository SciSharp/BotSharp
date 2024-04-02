using BotSharp.Abstraction.Messaging;
using BotSharp.Abstraction.Messaging.Enums;
using BotSharp.Abstraction.Messaging.Models.RichContent.Template;
using BotSharp.Abstraction.Messaging.Models.RichContent;
using System.Text.Json;
using System.Reflection;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace BotSharp.Core.Messaging;

public static class BotSharpMessageParser
{

    public static IRichMessage? ParseRichMessage(JsonElement root, JsonSerializerOptions options)
    {
        IRichMessage? res = null;
        JsonElement element;
        var jsonText = root.GetRawText();

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
            else if (richType == RichTypeEnum.GenericTemplate)
            {
                if (root.TryGetProperty("element_type", out element))
                {
                    var elementType = element.GetString();
                    var wrapperType = typeof(GenericTemplateMessage<>);
                    var genericType = Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(x => x.Name == elementType);

                    if (wrapperType != null && genericType != null)
                    {
                        var targetType = wrapperType.MakeGenericType(genericType);
                        res = JsonConvert.DeserializeObject(jsonText, targetType) as IRichMessage;
                    }
                }
            }
        }

        return res;
    }

    public static ITemplateMessage? ParseTemplateMessage(JsonElement root, JsonSerializerOptions options)
    {
        ITemplateMessage? res = null;
        JsonElement element;
        var jsonText = root.GetRawText();

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
            else if (templateType == TemplateTypeEnum.Generic)
            {
                if (root.TryGetProperty("element_type", out element))
                {
                    var elementType = element.GetString();
                    var wrapperType = typeof(GenericTemplateMessage<>);
                    var genericType = Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(x => x.Name == elementType);

                    if (wrapperType != null && genericType != null)
                    {
                        var targetType = wrapperType.MakeGenericType(genericType);
                        res = JsonConvert.DeserializeObject(jsonText, targetType) as ITemplateMessage;
                    }
                }
            }
        }

        return res;
    }
}
