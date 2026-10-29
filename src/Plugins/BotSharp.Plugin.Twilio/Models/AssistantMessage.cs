namespace BotSharp.Plugin.Twilio.Models
{
    public class AssistantMessage
    {
        public bool ConversationEnd { get; set; }
        public bool HumanIntervationNeeded { get; set; }
        public string Content { get; set; }
        public string MessageId { get; set; }
        public string? SpeechFileName { get; set; }
        public string Hints { get; set; }

        /// <summary>
        /// What of the reply is said once the turn is over must play to the end, as the flow asked when the reply came in
        /// </summary>
        public bool KeepUninterrupted { get; set; }
    }
}
