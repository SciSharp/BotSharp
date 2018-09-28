using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines.Articulate
{
    public class IntentModel
    {
        public int Id { get; set; }

        public string IntentName { get; set; }

        public string Agent { get; set; }

        public string Domain { get; set; }

        public bool UsePostFormat { get; set; }

        public bool UseWebhook { get; set; }

        public List<IntentExampleModel> Examples { get; set; }
    }
}
