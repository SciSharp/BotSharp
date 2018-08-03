using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BotSharp.Core.Adapters.Sebis
{
    public class SebisAgent
    {
        public String Id { get; set; }
        public String Name { get; set; }
        [JsonProperty("desc")]
        public String Description { get; set; }
        [JsonProperty("lang")]
        public String Language { get; set; }

        public List<SebisIntent> Sentences { get; set; }
    }
}
