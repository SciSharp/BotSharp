using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.RestApi.Integrations.FacebookMessenger
{
    public class WebhookEventEntry
    {
        public string Id { get; set; }
        public long Time { get; set; }
        public List<WebhookMessage> Messaging { get; set; }
    }
}
