using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using BotSharp.NLP;
using BotSharp.NLP.Classify;
using BotSharp.NLP.Txt2Vec;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Engines.BotSharp
{
    public class BotSharpNBayesClassifier : INlpTrain, INlpPredict
    {
        public IConfiguration Configuration { get; set; }
        public PipeSettings Settings { get; set; }

        public async Task<bool> Train(Agent agent, NlpDoc doc, PipeModel meta)
        {
            meta.Model = "classification-nb.model";

            string modelFileName = Path.Combine(Settings.ModelDir, meta.Model);

            var encoder = new OneHotEncoder();
            encoder.Sentences = doc.Sentences.Select(x => new NLP.Sentence
            {
                Label = x.Intent.Label,
                Text = x.Text,
                Words = x.Tokens
            }).ToList();
            encoder.EncodeAll();

            var options = new ClassifyOptions
            {
                TrainingCorpusDir = Path.Combine(Configuration.GetValue<String>("MachineLearning:dataDir"), "Text Classification", "cooking.stackexchange")
            };
            var classifier = new ClassifierFactory<NaiveBayesClassifier, SentenceFeatureExtractor>(options, SupportedLanguage.English);

            classifier.Train(encoder.Sentences);

            Console.WriteLine($"Saved model to {modelFileName}");
            meta.Meta = new JObject();
            meta.Meta["compiled at"] = "Sep 12, 2018";

            return true;
        }

        public async Task<bool> Predict(Agent agent, NlpDoc doc, PipeModel meta)
        {
            return true;
        }
    }
}
