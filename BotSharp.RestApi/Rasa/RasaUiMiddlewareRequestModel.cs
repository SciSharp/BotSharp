using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.RestApi.Rasa
{
    public class RasaUiMiddlewareRequestModel
    {
        [JsonProperty("ip_address")]
        public string IP { get; set; }

        public string Query { get; set; }

        [JsonProperty("event_type")]
        public string EventType { get; set; }

        /*[JsonProperty("event_data")]
        public T EventData { get; set; }*/
    }
}
