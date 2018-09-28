using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines.Articulate
{
    public class AgentModel
    {
        public int Id { get; set; }

        public string Status { get; set; }

        public string Timezone { get; set; }

        public string Language { get; set; }

        public string AgentName { get; set; }

        public bool UseWebhook { get; set; }

        public string Description { get; set; }

        public bool UsePostFormat { get; set; }

        public bool ExtraTrainingData { get; set; }

        public List<String> FallbackResponses { get; set; }

        public bool EnableModelsPerDomain { get; set; }

        public decimal DomainClassifierThreshold { get; set; }
    }
}
