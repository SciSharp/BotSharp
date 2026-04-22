namespace BotSharp.Abstraction.Messaging.Models.RichContent.Template;

public class EmbeddingTemplateMessage : IRichMessage, ITemplateMessage
{
    [JsonPropertyName("rich_type")]
    public string RichType => RichTypeEnum.EmbeddingTemplate;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("html_tag")]
    public string HtmlTag { get; set; } = "iframe";

    [JsonPropertyName("template_type")]
    public virtual string TemplateType { get; set; } = TemplateTypeEnum.Embedding;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}