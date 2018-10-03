using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Dialogflow.Models
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
