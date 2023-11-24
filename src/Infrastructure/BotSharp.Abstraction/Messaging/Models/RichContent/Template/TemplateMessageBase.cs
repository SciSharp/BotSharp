namespace BotSharp.Abstraction.Messaging.Models.RichContent.Template
{
    public class TemplateMessageBase<T>
    {
        [JsonIgnore]
        public string Text { get; set; }

        [JsonPropertyName("template_type")]
        public virtual string TemplateType => string.Empty;
        public T[] Elements { get; set; }
    }
}
