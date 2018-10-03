using BotSharp.Core.Engines;
using BotSharp.Platform.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Abstractions
{
    public interface INlpTrain : INlpPipeline
    {
        /// <summary>
        /// Process
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="doc">Intermediate result</param>
        /// <param name="meta">Meta data which is packed to model</param>
        /// <returns></returns>
        Task<bool> Train(AgentBase agent, NlpDoc doc, PipeModel meta);
    }
}
