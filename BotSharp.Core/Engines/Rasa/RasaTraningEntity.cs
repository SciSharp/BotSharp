using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Adapters.Rasa
{
    public class RasaTraningEntity
    {
        [JsonIgnore]
        public String EntityType { get; set; }

        [JsonProperty("value")]
        public String EntityValue { get; set; }

        public List<String> Synonyms { get; set; }
    }
}
