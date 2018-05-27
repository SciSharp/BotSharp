using BotSharp.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Adapters.Dialogflow
{
    public class DialogflowIntentResponseMessage : AIResponseMessageBase
    {
        public Object Speech { get; set; }
        public Object Payload { get; set; }
    }
}
