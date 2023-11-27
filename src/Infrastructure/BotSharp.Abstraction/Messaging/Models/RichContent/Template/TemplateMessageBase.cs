namespace BotSharp.Abstraction.Messaging.Models.RichContent.Template
{
    public class TemplateMessageBase<T> : RichMessageBase
    {
        [JsonIgnore]
        public string Text { get; set; }

        public override string Type => "template";

        [JsonPropertyName("template_type")]
        public virtual string TemplateType => string.Empty;

        public T[] Elements { get; set; }
    }
}
