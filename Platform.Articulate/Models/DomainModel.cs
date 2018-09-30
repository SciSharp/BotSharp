using BotSharp.Platform.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Articulate.Models
{
    public class DomainModel
    {
        public DomainModel()
        {
            Intents = new List<IntentModel>();
        }

        public string Id { get; set; }

        public string Agent { get; set; }

        public string DomainName { get; set; }

        public bool Enabled { get; set; }

        public bool ExtraTrainingData { get; set; }

        public decimal IntentThreshold { get; set; }

        public string Status { get; set; }

        public List<IntentModel> Intents { get; set; }
    }
}
