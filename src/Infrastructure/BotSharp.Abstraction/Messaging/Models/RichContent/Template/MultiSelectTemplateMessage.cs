using Newtonsoft.Json;

namespace BotSharp.Abstraction.Messaging.Models.RichContent.Template;

public class MultiSelectTemplateMessage : IRichMessage, ITemplateMessage
{
    [JsonPropertyName("rich_type")]
    [JsonProperty("rich_type")]
    public string RichType => RichTypeEnum.MultiSelectTemplate;

    [JsonPropertyName("text")]
    [JsonProperty("text")]
    [Translate]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("template_type")]
    [JsonProperty("template_type")]
    public string TemplateType => TemplateTypeEnum.MultiSelect;

    [JsonPropertyName("options")]
    [JsonProperty("options")]
    public List<OptionElement> Options { get; set; } = new List<OptionElement>();

    [JsonPropertyName("is_horizontal")]
    [JsonProperty("is_horizontal")]
    public bool IsHorizontal { get; set; }
}

public class OptionElement
{
    [Translate]
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Payload { get; set; }
}