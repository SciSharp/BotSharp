namespace BotSharp.Abstraction.Messaging;

public class RichMessageBase
{
    public virtual string Type { get; }

    public virtual string Text { get; set; }

    [JsonPropertyName("template_type")]
    public virtual string TemplateType { get; }
}
