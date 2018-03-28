using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Models
{
    [JsonObject]
    public class OriginalRequest
    {
        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("data")]
        public object Data { get; set; }
    }
}
