using BotSharp.Abstraction.Messaging.Enums;

namespace BotSharp.Abstraction.Messaging.Models.RichContent.Template;

public class MultiSelectTemplateMessage : IRichMessage, ITemplateMessage
{
    [JsonPropertyName("rich_type")]
    public string RichType => RichTypeEnum.MultiSelectTemplate;

    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("template_type")]
    public string TemplateType => TemplateTypeEnum.MultiSelect;
    public List<OptionElement> Options { get; set; } = new List<OptionElement>();
}

public class OptionElement
{
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Payload { get; set; }
}