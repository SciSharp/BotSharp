
namespace BotSharp.Abstraction.Messaging.Models.RichContent
{
    public class QuickReplyMessage : TextMessage
    {
        [JsonPropertyName("quick_replies")]
        public List<QuickReplyElement> QuickReplies { get; set; } = new List<QuickReplyElement>();
    }
}
