using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Articulate.Models
{
    public class DomainModel
    {
        public int Id { get; set; }

        public string Agent { get; set; }

        public string DomainName { get; set; }

        public bool Enabled { get; set; }

        public bool ExtraTrainingData { get; set; }

        public decimal IntentThreshold { get; set; }

        public string Status { get; set; }
    }
}
