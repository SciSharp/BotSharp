using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Channel.FacebookMessenger.Models
{
    public class WebhookMessage
    {
        public WebhookMessageSender Sender { get; set; }

        public WebhookMessageRecipient Recipient { get; set; }

        public long Timestamp { get; set; }

        public JObject Message { get; set; }
    }

    public class WebhookMessage<TWebhookMessage> where TWebhookMessage : IWebhookMessageBody
    {
        public WebhookMessageSender Sender { get; set; }

        public WebhookMessageRecipient Recipient { get; set; }

        public long Timestamp { get; set; }

        public TWebhookMessage Message { get; set; }
    }
}
