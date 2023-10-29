
namespace BotSharp.Abstraction.Messaging.Models.RichContent.Template
{
    public class AttachmentTemplate<T>
    {
        public string Type => "template";
        public T Payload { get; set; }
    }
}
