using BotSharp.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.RestApi.Rasa
{
    public class RasaTrainRequestModel
    {
        public string Project { get; set; }

        [JsonProperty("rasa_nlu_data")]
        public RasaTrainingData Corpus { get; set; }
    }
}
