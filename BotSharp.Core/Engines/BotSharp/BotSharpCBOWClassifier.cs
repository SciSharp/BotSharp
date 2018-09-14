using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using DotNetToolkit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BotSharp.Core.Engines.BotSharp
{
    public class BotSharpCBOWClassifier : INlpTrain, INlpPredict
    {
        public IConfiguration Configuration { get; set; }

        public PipeSettings Settings { get; set; }

        public async Task<bool> Predict(Agent agent, NlpDoc doc, PipeModel meta)
        {
            string modelFileName = Path.Combine(Settings.ModelDir, meta.Model);
            string predictFileName = Path.Combine(Settings.TempDir, "fasttext.txt");
            File.WriteAllText(predictFileName, doc.Sentences[0].Text);

            var output = CmdHelper.Run(Path.Combine(Settings.AlgorithmDir, "fasttext"), $"predict-prob \"{modelFileName}.bin\" \"{predictFileName}\"");

            File.Delete(predictFileName);

            if (!String.IsNullOrEmpty(output))
            {
                doc.Sentences[0].Intent = new TextClassificationResult
                {
                    Classifier = "FasttextClassifier",
                    Label = output.Split(' ')[0].Split(new string[] { "__label__" }, StringSplitOptions.None)[1],
                    Confidence = decimal.Parse(output.Split(' ')[1])
                };
            }

            return true;
        }

        public async Task<bool> Train(Agent agent, NlpDoc doc, PipeModel meta)
        {
            meta.Model = "classification-fasttext.model";

            string parsedTrainingDataFileName = Path.Combine(Settings.TempDir, $"classification-fasttext.parsed.txt");
            string modelFileName = Path.Combine(Settings.ModelDir, meta.Model);

            // assemble corpus
            StringBuilder corpus = new StringBuilder();
            agent.Corpus.UserSays.ForEach(x => corpus.AppendLine($"__label__{x.Intent} {x.Text}"));

            File.WriteAllText(parsedTrainingDataFileName, corpus.ToString());

            var output = CmdHelper.Run(Path.Combine(Settings.AlgorithmDir, "fasttext"), $"supervised -input \"{parsedTrainingDataFileName}\" -output \"{modelFileName}\"", false);

            Console.WriteLine($"Saved model to {modelFileName}");

            return true;
        }
    }
}
