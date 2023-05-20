using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Rasa.Models
{
    public class RasaTrainRequestViewModel
    {
        public string Project { get; set; }

        public string Model { get; set; }

        [JsonProperty("rasa_nlu_data")]
        public RasaTrainingData Corpus { get; set; }
    }
}
