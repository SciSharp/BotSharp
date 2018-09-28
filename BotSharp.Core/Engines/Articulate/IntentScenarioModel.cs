using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines.Articulate
{
    public class IntentScenarioModel
    {
        public int Id { get; set; }

        public string Domain { get; set; }

        public string Agent { get; set; }

        public string Intent { get; set; }

        public string ScenarioName { get; set; }

        public List<String> IntentResponses { get; set; }

        public List<SlotModel> Slots { get; set; }
    }
}
