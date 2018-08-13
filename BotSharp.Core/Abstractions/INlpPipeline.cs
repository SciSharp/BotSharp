using BotSharp.Core.Agents;
using BotSharp.Core.Engines;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Abstractions
{
    /// <summary>
    /// NLP process pipeline interface
    /// </summary>
    public interface INlpPipeline
    {
        IConfiguration Configuration { get; set; }

        /// <summary>
        /// Common settings for Pipeline
        /// </summary>
        PipeSettings Settings { get; set; }
    }
}
