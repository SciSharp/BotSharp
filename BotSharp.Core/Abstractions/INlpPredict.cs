using BotSharp.Core.Agents;
using BotSharp.Core.Engines;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Abstractions
{
    public interface INlpPredict : INlpPipeline
    {
        Task<bool> Predict(Agent agent, NlpDoc doc, PipeModel meta);
    }
}
