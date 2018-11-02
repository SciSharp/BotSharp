using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Channel.FacebookMessenger.Models
{
    public class WebhookTextMessage : IWebhookMessageBody
    {
        public string Mid { get; set; }
        public String Text { get; set; }
        [JsonProperty("quick_reply")]
        public WebhookMessageQuickReply QuickReply { get; set; }
    }
}
