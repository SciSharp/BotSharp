using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Dialogflow.Models
{
    public class DialogflowAgentImportModel
    {
        public String Id { get; set; }
        public String Name { get; set; }
        public String Description { get; set; }
        public Boolean Published { get; set; }

        public String DefaultTimezone { get; set; }
        public String Language { get; set; }

        public double MlMinConfidence { get; set; }
        public string CustomClassifierMode { get; set; }
    }
}
