using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;

namespace BotSharp.Core.Engines.BotSharp
{
    public class BotSharpNLU : BotEngineBase, IBotEngine
    {
        public override async Task Train(BotTrainOptions options)
        {
            var trainer = new BotTrainer(agent.Id, dc);
            await trainer.Train(agent, options);
        }
    }
}
