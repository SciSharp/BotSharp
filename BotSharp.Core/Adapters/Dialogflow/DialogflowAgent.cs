using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Adapters.Dialogflow
{
    public class DialogflowAgent
    {
        public String Id { get; set; }
        public String Name { get; set; }
        public String Description { get; set; }
        public Boolean Published { get; set; }

        public String Language { get; set; }
    }
}
