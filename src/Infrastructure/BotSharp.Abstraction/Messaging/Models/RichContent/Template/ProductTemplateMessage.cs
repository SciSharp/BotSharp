namespace BotSharp.Abstraction.Messaging.Models.RichContent.Template;

public class ProductTemplateMessage : TemplateMessageBase<ProductElement>, IRichMessage
{
    [JsonPropertyName("template_type")]
    public override string TemplateType => "product";
}

public class ProductElement
{
    public string Id { get; set; }
}
