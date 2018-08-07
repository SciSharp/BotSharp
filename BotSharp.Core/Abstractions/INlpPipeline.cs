using BotSharp.Core.Agents;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Abstractions
{
    /// <summary>
    /// NLP process pipeline interface
    /// </summary>
    public interface INlpPipeline
    {
        IConfiguration Configuration { get; set; }

        bool ProcessAsync(Agent agent, JObject data);
    }
}
