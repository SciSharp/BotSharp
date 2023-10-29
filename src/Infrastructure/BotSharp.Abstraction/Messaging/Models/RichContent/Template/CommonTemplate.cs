
namespace BotSharp.Abstraction.Messaging.Models.RichContent.Template
{
    public class CommonTemplate<T>
    {
        [JsonPropertyName("template_type")]
        public string TemplateType { get; set; } = string.Empty;
        public T Elements { get; set; }
    }
}
