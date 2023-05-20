using BotSharp.Core.Intents;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Adapters.Sebis
{
    public class SebisIntent
    {
        public string Text { get; set; }
        [JsonProperty("intent")]
        public string Name { get; set; }
        public List<SebisIntentExpressionPart> Entities { get; set; }
    }
}
