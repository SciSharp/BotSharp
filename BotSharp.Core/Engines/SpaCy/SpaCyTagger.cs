using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines.SpaCy
{
    class SpaCyTagger : INlpPipeline
    {
        public IConfiguration Configuration { get; set; }

        public bool Process(Agent agent, JObject data)
        {
            throw new NotImplementedException();
        }
    }
}
