using BotSharp.Core.Adapters.Rasa;
using BotSharp.Core.Engines.Rasa;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Models
{
    public class RasaTrainingData
    {
        [JsonProperty("common_examples")]
        public List<RasaIntentExpression> UserSays { get; set; }

        [JsonProperty("entity_synonyms")]
        public List<RasaTrainingEntity> Entities { get; set; }

        [JsonProperty("regex_features")]
        public List<RasaTrainingRegex> Regex { get; set; }
    }
}
