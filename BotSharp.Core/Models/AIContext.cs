using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Models
{
    [JsonObject]
    public class AIContext
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("parameters")]
        public Dictionary<string, string> Parameters { get; set; }

        /// <summary>
        /// Lifespan of the context measured in requests````
        /// </summary>
        [JsonProperty("lifespan")]
        public int Lifespan { get; set; }

        public AIContext()
        {
        }
    }
}
