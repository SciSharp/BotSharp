using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Engines.Jieba.NET
{
    public class JiebaTokenizer : INlpTrain, INlpPredict
    {
        public IConfiguration Configuration { get; set; }
        public PipeSettings Settings { get; set; }

        public Task<bool> Predict(Agent agent, NlpDoc doc, PipeModel meta)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Train(Agent agent, NlpDoc doc, PipeModel meta)
        {
            throw new NotImplementedException();
        }
    }
}
