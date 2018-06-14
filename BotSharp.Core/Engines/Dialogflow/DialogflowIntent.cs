using BotSharp.Core.Intents;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Adapters.Dialogflow
{
    public class DialogflowIntent
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool Auto { get; set; }
        /// <summary>
        /// Input Contexts
        /// </summary>
        public List<String> ContextList { get; set; }

        public List<DialogflowIntentExpression> UserSays { get; set; }
        public List<DialogflowIntentResponse> Responses { get; set; }
        public int Priority { get; set; }
        public bool WebhookUsed { get; set; }
        public bool FallbackIntent { get; set; }
        public List<DialogflowIntentEvent> Events { get; set; }
    }
}
