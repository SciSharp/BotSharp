using BotSharp.Abstraction.Messaging.Enums;
using BotSharp.Abstraction.Messaging.Models.RichContent.Template;
using BotSharp.Abstraction.Messaging.Models.RichContent;
using System.Text.Json;
using System.Reflection;
using Newtonsoft.Json;

namespace BotSharp.Abstraction.Messaging;

public static class BotSharpMessageParser
{

    public static IRichMessage? ParseRichMessage(JsonElement root)
    {
        IRichMessage? res = null;
        Type? targetType = null;
        JsonElement element;
        var jsonText = root.GetRawText();

        if (root.TryGetProperty("rich_type", out element))
        {
            var richType = element.GetString();
            if (richType == RichTypeEnum.ButtonTemplate)
            {
                targetType = typeof(ButtonTemplateMessage);
            }
            else if (richType == RichTypeEnum.MultiSelectTemplate)
            {
                targetType = typeof(MultiSelectTemplateMessage);
            }
            else if (richType == RichTypeEnum.QuickReply)
            {
                targetType = typeof(QuickReplyMessage);
            }
            else if (richType == RichTypeEnum.CouponTemplate)
            {
                targetType = typeof(CouponTemplateMessage);
            }
            else if (richType == RichTypeEnum.Text)
            {
                targetType = typeof(TextMessage);
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
                        targetType = wrapperType.MakeGenericType(genericType);
                    }
                }
            }
        }

        if (targetType != null)
        {
            res = JsonConvert.DeserializeObject(jsonText, targetType) as IRichMessage;
        }

        return res;
    }

    public static ITemplateMessage? ParseTemplateMessage(JsonElement root)
    {
        ITemplateMessage? res = null;
        Type? targetType = null;
        JsonElement element;
        var jsonText = root.GetRawText();

        if (root.TryGetProperty("template_type", out element))
        {
            var templateType = element.GetString();
            if (templateType == TemplateTypeEnum.Button)
            {
                targetType = typeof(ButtonTemplateMessage);
            }
            else if (templateType == TemplateTypeEnum.MultiSelect)
            {
                targetType = typeof(MultiSelectTemplateMessage);
            }
            else if (templateType == TemplateTypeEnum.Coupon)
            {
                targetType = typeof(CouponTemplateMessage);
            }
            else if (templateType == TemplateTypeEnum.Product)
            {
                targetType = typeof(ProductTemplateMessage);
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
                        targetType = wrapperType.MakeGenericType(genericType);
                        res = JsonConvert.DeserializeObject(jsonText, targetType) as ITemplateMessage;
                    }
                }
            }
        }

        if (targetType != null)
        {
            res = JsonConvert.DeserializeObject(jsonText, targetType) as ITemplateMessage;
        }

        return res;
    }
}
