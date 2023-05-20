using System;
using System.Collections.Generic;
using System.Text;
using BotSharp.Core.Engines;
using BotSharp.Platform.Models;
using Newtonsoft.Json;

namespace BotSharp.Core.Adapters.Sebis
{
    public class SebisIntentExpressionPart : TrainingIntentExpressionPart
    {
        [JsonProperty("stop")]
        public new int End { get; set; }
        [JsonProperty("text")]
        public new String Value { get; set; }
    }
}