using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bot.Rasa.Models
{
    public class RasaTrainingData
    {
        [JsonProperty("common_examples")]
        public List<UserSay> UserSays { get; set; }
    }
}
