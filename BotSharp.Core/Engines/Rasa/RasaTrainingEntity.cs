using BotSharp.Core.Engines;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Adapters.Rasa
{
    public sealed class RasaTrainingEntity : TrainingEntity
    {
        [JsonIgnore]
        public override String EntityType { get; set; }

        [JsonProperty("value")]
        public override String EntityValue { get; set; }
    }
}
