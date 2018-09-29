using BotSharp.Platform.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Articulate.Models
{
    public class IntentModel : IntentBase
    {
        [JsonProperty("IntentName")]
        public new string Name { get; set; }

        public string Agent { get; set; }

        public string Domain { get; set; }

        public bool UsePostFormat { get; set; }

        public bool UseWebhook { get; set; }

        public List<IntentExampleModel> Examples { get; set; }

        public IntentResponseBase Response { get; set; }
    }
}
