using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Channel.FacebookMessenger.Models
{
    public class WebhookEvent
    {
        public string Object { get; set; }
        public List<WebhookEventEntry> Entry { get; set; }
    }


}
