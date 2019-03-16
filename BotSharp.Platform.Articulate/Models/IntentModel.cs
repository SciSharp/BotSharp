using BotSharp.Platform.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Articulate.Models
{
    public class IntentModel : IntentBase
    {
        public string IntentName { get; set; }

        public string Agent { get; set; }

        public string Domain { get; set; }

        public bool UsePostFormat { get; set; }

        public bool UseWebhook { get; set; }

        /// <summary>
        /// User says
        /// </summary>
        public List<IntentExampleModel> Examples { get; set; }

        /// <summary>
        /// Intent responses
        /// </summary>
        public ScenarioModel Scenario { get; set; }
    }
}
