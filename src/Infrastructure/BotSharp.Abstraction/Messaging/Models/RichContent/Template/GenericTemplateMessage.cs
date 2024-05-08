using BotSharp.Abstraction.Messaging.Enums;
using Newtonsoft.Json;

namespace BotSharp.Abstraction.Messaging.Models.RichContent.Template;

public class GenericTemplateMessage<T> : IRichMessage, ITemplateMessage
{
    [JsonPropertyName("rich_type")]
    [JsonProperty("rich_type")]
    public string RichType => RichTypeEnum.GenericTemplate;

    [JsonPropertyName("text")]
    [JsonProperty("text")]
    [Translate]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("template_type")]
    [JsonProperty("template_type")]
    public virtual string TemplateType { get; set; } = TemplateTypeEnum.Generic;

    [JsonPropertyName("elements")]
    [JsonProperty("elements")]
    public List<T> Elements { get; set; } = new List<T>();

    [JsonPropertyName("is_horizontal")]
    [JsonProperty("is_horizontal")]
    public bool IsHorizontal { get; set; }

    [JsonPropertyName("is_popup")]
    [JsonProperty("is_popup")]
    public bool IsPopup { get; set; }

    [JsonPropertyName("element_type")]
    [JsonProperty("element_type")]
    public string ElementType => typeof(T).Name;
}

public class GenericElement
{
    [Translate]
    public string Title { get; set; }

    [Translate]
    public string Subtitle { get; set; }

    [JsonPropertyName("image_url")]
    [JsonProperty("image_url")]
    public string ImageUrl { get; set; }

    [JsonPropertyName("default_action")]
    [JsonProperty("default_action")]
    public ElementAction DefaultAction { get; set; }
    public ElementButton[] Buttons { get; set; }
}
