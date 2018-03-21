using Bot.Rasa.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bot.Rasa.Adapters.Dialogflow
{
    public class DialogflowIntentResponseMessage : AIResponseMessageBase
    {
        public string Lang { get; set; }
        public Object Speech { get; set; }
        public Object Payload { get; set; }
    }
}
