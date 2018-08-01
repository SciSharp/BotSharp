using System;
using System.Collections.Generic;
using System.Text;
using BotSharp.Core.Models;

namespace BotSharp.Core.Engines.BotSharp
{
    public class BotSharpAi : BotEngineBase, IBotPlatform
    {
        public AIResponse TextRequest(AIRequest request)
        {
            throw new NotImplementedException();
        }
        public override void Train()
        {
            agent.Corpus = GetIntentExpressions();
            var trainer = new BotTrainer(agent.Id, dc);
            trainer.Train(agent);
        }
    }
}
