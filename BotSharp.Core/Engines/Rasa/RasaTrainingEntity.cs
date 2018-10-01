using BotSharp.Core.Engines;
using BotSharp.Platform.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Adapters.Rasa
{
    public sealed class RasaTrainingEntity : TrainingEntity
    {
        [JsonProperty("value")]
        public override String Entity { get; set; }
    }
}
