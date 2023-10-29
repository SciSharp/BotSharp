
namespace BotSharp.Abstraction.Messaging.Models.RichContent.Template
{
    public class ButtonTemplate : TextMessage
    {
        [JsonPropertyName("template_type")]
        public string TemplateType => "button"; 
        public List<ButtonElement> Buttons { get; set; } = new List<ButtonElement>();
    }

    public class ButtonElement
    {
        public string Type { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string Title { get; set; } = string.Empty;
    }
}
