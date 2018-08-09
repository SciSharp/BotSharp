using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using DotNetToolkit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Engines.Classifiers
{
    public class FasttextClassifier : INlpPipeline
    {
        public IConfiguration Configuration { get; set; }

        public Task<bool> Predict(Agent agent, JObject data, PipeModel meta)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> Train(Agent agent, JObject data, PipeModel meta)
        {
            meta.Model = "classification-fasttext.model";
            var algorithmDir = Path.Join(AppDomain.CurrentDomain.GetData("ContentRootPath").ToString(), "Algorithms");
            var dirTrain = Path.Join(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "TrainingFiles", agent.Id);
            var dirModel = Path.Join(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "ModelFiles", agent.Id);

            string parsedTrainingDataFileName = Path.Join(dirTrain, $"classification-fasttext.parsed.txt");
            string modelFileName = Path.Join(dirModel, meta.Model);

            // assemble corpus
            StringBuilder corpus = new StringBuilder();
            agent.Corpus.UserSays.ForEach(x => corpus.AppendLine($"__label__{x.Intent} {x.Text}"));

            File.WriteAllText(parsedTrainingDataFileName, corpus.ToString());

            var output = CmdHelper.Run(Path.Join(algorithmDir, "fasttext"), $"supervised -input {parsedTrainingDataFileName} -output {modelFileName}");

            Console.WriteLine($"Saved model to {modelFileName}");
            meta.Meta = new JObject();
            meta.Meta["compiled at"] = "Aug 3, 2018";


            return true;
        }
    }
}
