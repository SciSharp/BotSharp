using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Models
{
    public class RasaResponse
    {
        public RasaResponseIntent Intent { get; set; }

        [JsonProperty("intent_ranking")]
        public List<RasaResponseIntent> IntentRanking { get; set; }

        public List<RasaResponseEntity> Entities { get; set; }

        public String Text { get; set; }

        public String Project { get; set; }

        public String Model { get; set; }
    }

    public class RasaResponseIntent
    {
        public String Name { get; set; }

        public Decimal Confidence { get; set; }
    }
}
