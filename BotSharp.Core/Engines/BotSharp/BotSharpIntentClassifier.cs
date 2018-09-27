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
    public class BotSharpIntentClassifier : INlpTrain, INlpPredict
    {
        public IConfiguration Configuration { get; set; }
        public PipeSettings Settings { get; set; }
        private ClassifierFactory<SentenceFeatureExtractor> _classifier;

        public async Task<bool> Train(Agent agent, NlpDoc doc, PipeModel meta)
        {
            Init(meta);

            var sentences = doc.Sentences.Select(x => new Sentence
            {
                Label = x.Intent.Label,
                Text = x.Text,
                Words = x.Tokens
            }).ToList();

            _classifier.Train(sentences);

            Console.WriteLine($"Saved model to {Settings.ModelDir}");

            return true;
        }

        public async Task<bool> Predict(Agent agent, NlpDoc doc, PipeModel meta)
        {
            Init(meta);

            var sentence = doc.Sentences.Select(s => new Sentence
            {
                Text = s.Text,
                Words = s.Tokens
            }).First();


            var result = _classifier.Classify(sentence);

            doc.Sentences[0].Intent = new TextClassificationResult
            {
                Classifier = "BotSharpIntentClassifier",
                Label = result.First().Item1,
                Confidence = (decimal)result.First().Item2
            };

            return true;
        }

        private void Init(PipeModel meta)
        {
            if (_classifier == null)
            {
                meta.Model = "intent.model";

                var options = new ClassifyOptions
                {
                    ModelFilePath = Path.Combine(Settings.ModelDir, meta.Model),
                    ModelDir = Settings.ModelDir,
                    ModelName = meta.Model
                };

                _classifier = new ClassifierFactory<SentenceFeatureExtractor>(options, SupportedLanguage.English);

                string classifierName = Configuration.GetValue<String>($"classifer");

                _classifier.GetClassifer(classifierName);
            }
        }
    }
}
