using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Engines.BotSharp
{
    public class BotSharpProvider : INlpProvider
    {
        public IConfiguration Configuration { get; set; }
        public PipeSettings Settings { get; set; }

        public async Task<bool> Load(Agent agent, PipeModel meta)
        {
            meta.Meta = JObject.FromObject(new { version = "0.1.0" });

            return true;
        }
    }
}
