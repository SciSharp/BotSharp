using BotSharp.Core.Engines;
using BotSharp.Platform.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Rasa.Models
{
    public sealed class RasaTrainingEntity
    {
        [JsonProperty("value")]
        public String Entity { get; set; }

        public List<string> Synonyms { get; set; }
    }
}
