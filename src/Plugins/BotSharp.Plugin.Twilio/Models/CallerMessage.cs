namespace BotSharp.Plugin.Twilio.Models
{
    public class CallerMessage
    {
        public string SessionId { get; set; }
        public int SeqNumber { get; set; }
        public string Content { get; set; }
        public string From { get; set; }

        public override string ToString()
        {
            return $"{SessionId}-{SeqNumber}";
        }
    }
}
