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

        /// <summary>
        /// Process
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="data">Intermediate result</param>
        /// <param name="meta">Meta data which is packed to model</param>
        /// <returns></returns>
        Task<bool> Train(Agent agent, JObject data, PipeModel meta);
        Task<bool> Predict(Agent agent, JObject data, PipeModel meta);
    }
}
