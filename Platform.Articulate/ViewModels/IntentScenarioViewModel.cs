using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Articulate.Models
{
    public class IntentScenarioViewModel : ScenarioModel
    {
        public string Domain { get; set; }

        public string Agent { get; set; }

        public string Intent { get; set; }
    }
}
