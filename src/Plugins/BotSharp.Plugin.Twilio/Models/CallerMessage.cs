using Microsoft.Extensions.Primitives;

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
        public KeyValuePair<string, StringValues>[] RequestHeaders { get; set; }

        public override string ToString()
        {
            return $"{ConversationId}-{SeqNumber}";
        }
    }
}
