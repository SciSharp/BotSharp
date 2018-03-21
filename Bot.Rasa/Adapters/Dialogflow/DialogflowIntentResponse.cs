using Bot.Rasa.Intents;
using Bot.Rasa.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bot.Rasa.Adapters.Dialogflow
{
    public class DialogflowIntentResponse
    {
        public string Id { get; set; }
        public bool ResetContexts { get; set; }
        public string Action { get; set; }

        public List<AIContext> AffectedContexts { get; set; }

        public List<DialogflowIntentResponseParameter> Parameters { get; set; }

        public List<DialogflowIntentResponseMessage> MessageList { get; set; }

        public DialogflowIntentResponse()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
