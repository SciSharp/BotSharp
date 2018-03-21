using System;
using System.Collections.Generic;
using System.Text;

namespace Bot.Rasa.Adapters.Dialogflow
{
    public class DialogflowIntentResponseParameter
    {
        public string Id { get; set; }
        public bool Required { get; set; }
        public string DataType { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsList { get; set; }
    }
}
