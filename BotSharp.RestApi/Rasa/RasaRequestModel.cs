using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.RestApi.Rasa
{
    public class RasaRequestModel
    {
        [JsonProperty("q")]
        public string Text { get; set; }

        public string Project { get; set; }

        public string Model { get; set; }
    }
}
