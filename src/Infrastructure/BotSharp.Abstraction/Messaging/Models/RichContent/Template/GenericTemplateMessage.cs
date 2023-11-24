namespace BotSharp.Abstraction.Messaging.Models.RichContent.Template;

public class GenericTemplateMessage : TemplateMessageBase<GenericElement>, IRichMessage, ITemplateMessage
{
    [JsonPropertyName("template_type")]
    public override string TemplateType => "generic";
}

public class GenericElement
{
    public string Title { get; set; }
    public string Subtitle { get; set; }
    [JsonPropertyName("image_url")]
    public string ImageUrl { get; set; }
    [JsonPropertyName("default_action")]
    public ElementAction DefaultAction { get; set; }
    public ElementButton[] Buttons { get; set; }
}
