using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Core.Models;

namespace BotSharp.Core.Engines.BotSharp
{
    public class BotSharpAi : BotEngineBase, IBotPlatform
    {
        public override async Task Train(BotTrainOptions options)
        {
            agent.Corpus = GetIntentExpressions(agent);
            var trainer = new BotTrainer(agent.Id, dc);
            await trainer.Train(agent, options);
        }
    }
}
