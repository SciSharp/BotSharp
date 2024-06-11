using BotSharp.Abstraction.Messaging.Models.RichContent.Template;
using BotSharp.Abstraction.Messaging.Models.RichContent;
using System.Text.Json;
using System.Reflection;
using System.Linq;

namespace BotSharp.Abstraction.Messaging;

public static class BotSharpMessageParser
{
    private static Dictionary<string, Type> GenericTemplateTypeMap = new();
    private static Dictionary<string, Type> ElementTypeMap = new();
    private static Dictionary<string, Type> NonGenericTemplateTypeMap = new();

    static BotSharpMessageParser()
    {
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .ToList();
        ElementTypeMap = types
            .Where(type => type.Name.EndsWith("Element"))
            .ToDictionary(k => k.Name, v => v);

        var richMessageTypes = types
            .Where(type => typeof(IRichMessage).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .ToDictionary(k => GetRichTypeValue(k), v => v);
        GenericTemplateTypeMap = richMessageTypes.Where(p => p.Value.IsGenericType).ToDictionary(k => k.Key, v => v.Value);
        NonGenericTemplateTypeMap = richMessageTypes.Where(p => !p.Value.IsGenericType).ToDictionary(k => k.Key, v => v.Value);
    }

    public static IRichMessage? ParseRichMessage(JsonElement root, JsonSerializerOptions options)
    {
        IRichMessage? res = null;
        Type? targetType = null;
        var jsonText = root.GetRawText();

        if (!root.TryGetProperty("rich_type", out var richTypeElement)) return res;

        string? richType = richTypeElement.GetString();
        if (GenericTemplateTypeMap.TryGetValue(richType, out var wrapperType))
        {
            if (root.TryGetProperty("element_type", out var elementTypeElement))
            {
                string? elementType = elementTypeElement.GetString();
                targetType = CreateGenericElementType(wrapperType, elementType);
            }
        }
        else if (NonGenericTemplateTypeMap.TryGetValue(richType, out targetType))
        {
            // targetType is already set by the dictionary lookup
        }

        if (targetType != null)
        {
            res = JsonSerializer.Deserialize(jsonText, targetType, options) as IRichMessage;
        }

        return res;
    }

    private static Type? CreateGenericElementType(Type wrapperType, string elementTypeName)
    {
        if (wrapperType != null && ElementTypeMap.TryGetValue(elementTypeName, out var elementType))
        {
            return wrapperType.MakeGenericType(elementType);
        }

        return null;
    }

    private static string GetRichTypeValue(Type type)
    {
        var richTypeProperty = type.GetProperty("RichType", BindingFlags.Public | BindingFlags.Instance);
        if (richTypeProperty != null && richTypeProperty.PropertyType == typeof(string))
        {
            return CreateRichMessage(type)?.RichType;
        }
        return null;
    }

    private static dynamic CreateRichMessage(Type type)
    {
        if (!type.IsGenericType)
        {
            return Activator.CreateInstance(type);
        }
        else
        {
            var genericType = type.MakeGenericType(typeof(object));
            return Activator.CreateInstance(genericType);
        }
    }

    public static ITemplateMessage? ParseTemplateMessage(JsonElement root, JsonSerializerOptions options)
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
                    }
                }
            }
        }

        if (targetType != null)
        {
            res = JsonSerializer.Deserialize(jsonText, targetType, options) as ITemplateMessage;
        }

        return res;
    }
}
