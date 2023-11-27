namespace BotSharp.Abstraction.Messaging.Models.RichContent
{
    public class QuickReplyMessage : RichMessageBase, IRichMessage
    {
        public override string Type => "quick reply";

        [JsonPropertyName("quick_replies")]
        public List<QuickReplyElement> QuickReplies { get; set; } = new List<QuickReplyElement>();
    }
}
