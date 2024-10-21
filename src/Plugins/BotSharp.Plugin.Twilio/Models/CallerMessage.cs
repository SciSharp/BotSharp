namespace BotSharp.Plugin.Twilio.Models
{
    public class CallerMessage
    {
        public string ConversationId { get; set; }
        public int SeqNumber { get; set; }
        public string Content { get; set; }
        public string Digits { get; set; }
        public string From { get; set; }
        public Dictionary<string, string> States { get; set; } = new();

        public override string ToString()
        {
            return $"{ConversationId}-{SeqNumber}";
        }
    }
}
