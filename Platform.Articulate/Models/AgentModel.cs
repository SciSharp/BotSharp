using BotSharp.Platform.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Articulate.Models
{
    public class AgentModel : AgentBase
    {
        public AgentModel()
        {
            Domains = new List<DomainModel>();
            Entities = new List<EntityBase>();
        }

        public string Status { get; set; }

        public string Timezone { get; set; }

        public string AgentName { get; set; }

        public bool UseWebhook { get; set; }

        public bool UsePostFormat { get; set; }

        public bool ExtraTrainingData { get; set; }

        public List<String> FallbackResponses { get; set; }

        public bool EnableModelsPerDomain { get; set; }

        public decimal DomainClassifierThreshold { get; set; }

        public List<DomainModel> Domains { get; set; }

        public List<EntityBase> Entities { get; set; }
    }
}
