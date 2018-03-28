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
    }
}
