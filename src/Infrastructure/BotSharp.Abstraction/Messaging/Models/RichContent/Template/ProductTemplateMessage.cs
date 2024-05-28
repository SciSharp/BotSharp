namespace BotSharp.Abstraction.Messaging.Models.RichContent.Template;

public class ProductTemplateMessage : IRichMessage, ITemplateMessage
{
    [JsonPropertyName("rich_type")]
    public string RichType => RichTypeEnum.GenericTemplate;

    [JsonPropertyName("text")]
    [Translate]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("template_type")]
    public string TemplateType => TemplateTypeEnum.Product;
}

public class ProductElement
{
    public string Id { get; set; }
}
