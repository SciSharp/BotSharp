
namespace BotSharp.Abstraction.Messaging.Models.RichContent
{
    public class SenderActionModel
    {
        /* 
         * Requests to display sender action should only include the sender_action parameter and the recipient object.
         * All other Send API properties, such as text and templates, should be sent in a separate request.
        */
        public Recipient Recipient { get; set; } = new Recipient();

        [JsonPropertyName("sender_action")]
        public string SenderAction { get; set; } = string.Empty;
    }
}
