using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Adapters.Dialogflow
{
    public class DialogflowIntentResponseParameter
    {
        public DialogflowIntentResponseParameter()
        {
            PromptList = new List<DialogflowIntentResponseParameterPrompt>();
        }
        public string Id { get; set; }
        public bool Required { get; set; }
        public string DataType { get; set; }
        public string DefaultValue { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsList { get; set; }

        public List<DialogflowIntentResponseParameterPrompt> PromptList { get; set; }
    }
}
