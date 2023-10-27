namespace BotSharp.Abstraction.Messaging.Models.RichContent
{
    public class RichContent<T>
    {
        public Recipient? Recipient { get; set; }
        /// <summary>
        /// RESPONSE
        /// </summary>
        [JsonPropertyName("messaging_type")]
        public string MessageingType { get; set; } = string.Empty;
        public T Message { get; set; }
    }
}
