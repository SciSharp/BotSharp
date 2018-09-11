using BotSharp.Core.Adapters.Rasa;
using BotSharp.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines.Rasa
{
    public class RasaAgent
    {
        public String Id { get; set; }
        public String Name { get; set; }

        [JsonProperty("common_examples")]
        public List<RasaIntentExpression> UserSays { get; set; }

        [JsonProperty("entity_synonyms")]
        public List<RasaTrainingEntity> Entities { get; set; }

        [JsonProperty("regex_features")]
        public List<RasaTrainingRegex> Regex { get; set; }
    }
}
