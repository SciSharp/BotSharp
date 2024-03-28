using BotSharp.Abstraction.Messaging;
using BotSharp.Abstraction.Messaging.Enums;
using BotSharp.Abstraction.Messaging.Models.RichContent.Template;
using BotSharp.Abstraction.Messaging.Models.RichContent;
using System.Text.Json;

namespace BotSharp.Core.Messaging;

public class MessageParser
{
    public MessageParser()
    {
    }

    public IRichMessage? ParseRichMessage(string richType, string jsonText, JsonElement root, JsonSerializerOptions options)
    {
        IRichMessage? res = null;

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
            if (root.TryGetProperty("element_type", out var element))
            {
                var elementType = element.GetString();
                if (elementType == typeof(GenericElement).Name)
                {
                    res = JsonSerializer.Deserialize<GenericTemplateMessage<GenericElement>>(jsonText, options);
                }
                else if (elementType == typeof(ButtonElement).Name)
                {
                    res = JsonSerializer.Deserialize<GenericTemplateMessage<ButtonElement>>(jsonText, options);
                }
            }
        }

        return res;
    }

    public ITemplateMessage? ParseTemplateMessage(string templateType, string jsonText, JsonElement root, JsonSerializerOptions options)
    {
        ITemplateMessage? res = null;

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
            if (root.TryGetProperty("element_type", out var element))
            {
                var elementType = element.GetString();
                if (elementType == typeof(GenericElement).Name)
                {
                    res = JsonSerializer.Deserialize<GenericTemplateMessage<GenericElement>>(jsonText, options);
                }
                else if (elementType == typeof(ButtonElement).Name)
                {
                    res = JsonSerializer.Deserialize<GenericTemplateMessage<ButtonElement>>(jsonText, options);
                }
            }
        }

        return res;
    }
}
