
namespace BotSharp.Abstraction.Messaging.Models.RichContent.Template
{
    public class CommonTemplate<T>
    {
        public string TemplateType { get; set; } = string.Empty;
        public T Elements { get; set; }
    }
}
