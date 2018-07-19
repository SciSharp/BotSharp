using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.RestApi.Integrations.FacebookMessenger
{
    public class WebhookEvent
    {
        public string Object { get; set; }
        public List<WebhookEventEntry> Entry { get; set; }
    }


}
