namespace BotSharp.Abstraction.Messaging.Models.RichContent.Template;

public class GenericTemplateMessage<T> : IRichMessage, ITemplateMessage
{
    [JsonPropertyName("rich_type")]
    public string RichType => RichTypeEnum.GenericTemplate;

    /// <summary>
    /// Use model refined content if leaving blank
    /// </summary>
    [JsonPropertyName("text")]
    [Translate]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("template_type")]
    public virtual string TemplateType { get; set; } = TemplateTypeEnum.Generic;

    [JsonPropertyName("elements")]
    public List<T> Elements { get; set; } = new List<T>();

    [JsonPropertyName("is_horizontal")]
    public bool IsHorizontal { get; set; }

    [JsonPropertyName("is_popup")]
    public bool IsPopup { get; set; }

    [JsonPropertyName("element_type")]
    public string ElementType => typeof(T).Name;
}

public class GenericElement
{
    [Translate]
    public string Title { get; set; }

    [Translate]
    public string Subtitle { get; set; }

    [Translate]
    public string Text { get; set; }

    [JsonPropertyName("image_url")]
    public string ImageUrl { get; set; }

    [JsonPropertyName("default_action")]
    public ElementAction DefaultAction { get; set; }
    public ElementButton[] Buttons { get; set; }
}
