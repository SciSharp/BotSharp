using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Core;
using BotSharp.Core.Engines;
using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using BotSharp.Platform.Models.AiRequest;
using BotSharp.Platform.Models.AiResponse;
using Platform.Dialogflow.Models;

namespace Platform.Dialogflow
{
    public class DialogflowAi<TAgent> :
        PlatformBuilderBase<TAgent>,
        IPlatformBuilder<TAgent>
        where TAgent : AgentModel
    {
        public TrainingCorpus ExtractorCorpus(TAgent agent)
        {
            throw new NotImplementedException();
        }

        public AiResponse TextRequest(AiRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Train(TAgent agent, TrainingCorpus corpus)
        {
            throw new NotImplementedException();
        }
    }
}
