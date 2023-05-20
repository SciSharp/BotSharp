using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Rasa.Models
{
    public class RasaAgentImportModel
    {
        [JsonProperty("rasa_nlu_data")]
        public AgentModel Data { get; set; }
    }
}
