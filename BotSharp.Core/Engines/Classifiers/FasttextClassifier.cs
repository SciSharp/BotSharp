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

        public PipeSettings Settings { get; set; }

        public async Task<bool> Predict(Agent agent, JObject data, PipeModel meta)
        {
            string modelFileName = Path.Join(Settings.ModelDir, meta.Model);
            string predictFileName = Path.Join(Settings.PredictDir, "test.txt");
            var output = CmdHelper.Run(Path.Join(Settings.AlgorithmDir, "fasttext"), $"predict-prob {modelFileName}.bin {predictFileName}", false);

            data["Intent"] = JObject.FromObject(new { Name = output.Split(' ')[0].Split("__label__")[1], Confidence = output.Split(' ')[1] });

            return true;
        }

        public async Task<bool> Train(Agent agent, JObject data, PipeModel meta)
        {
            meta.Model = "classification-fasttext.model";

            string parsedTrainingDataFileName = Path.Join(Settings.TrainDir, $"classification-fasttext.parsed.txt");
            string modelFileName = Path.Join(Settings.ModelDir, meta.Model);

            // assemble corpus
            StringBuilder corpus = new StringBuilder();
            agent.Corpus.UserSays.ForEach(x => corpus.AppendLine($"__label__{x.Intent} {x.Text}"));

            File.WriteAllText(parsedTrainingDataFileName, corpus.ToString());

            var output = CmdHelper.Run(Path.Join(Settings.AlgorithmDir, "fasttext"), $"supervised -input {parsedTrainingDataFileName} -output {modelFileName}");

            Console.WriteLine($"Saved model to {modelFileName}");
            meta.Meta = new JObject();
            meta.Meta["compiled at"] = "Aug 3, 2018";


            return true;
        }
    }
}
