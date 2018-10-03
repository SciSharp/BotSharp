using BotSharp.Core.Engines;
using BotSharp.Platform.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Abstractions
{
    public interface INlpProvider : INlpPipeline
    {
        Task<bool> Load(AgentBase agent, PipeModel meta);
    }
}
