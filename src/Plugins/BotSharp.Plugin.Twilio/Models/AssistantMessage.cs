namespace BotSharp.Plugin.Twilio.Models
{
    public class AssistantMessage
    {
        public bool ConversationEnd { get; set; }
        public bool HumanIntervationNeeded { get; set; }
        public string Content { get; set; }
        public string MessageId { get; set; }
        public string SpeechFileName { get; set; }
        public string Hints { get; set; }
    }
}
