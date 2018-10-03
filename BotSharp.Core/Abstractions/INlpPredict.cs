using BotSharp.Core.Engines;
using BotSharp.Platform.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Abstractions
{
    public interface INlpPredict : INlpPipeline
    {
        Task<bool> Predict(AgentBase agent, NlpDoc doc, PipeModel meta);
    }
}
