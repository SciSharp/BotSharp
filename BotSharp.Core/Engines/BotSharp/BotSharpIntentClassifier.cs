using BotSharp.Core.Abstractions;
using CherubNLP;
using CherubNLP.Classify;
using BotSharp.Platform.Models;
using BotSharp.Platform.Models.AiResponse;
using BotSharp.Platform.Models.MachineLearning;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BotSharp.Core.Engines.BotSharp
{
    public class BotSharpIntentClassifier : INlpTrain, INlpPredict
    {
        public IConfiguration Configuration { get; set; }
        public PipeSettings Settings { get; set; }
        private ClassifierFactory<SentenceFeatureExtractor> _classifier;

        public async Task<bool> Train(AgentBase agent, NlpDoc doc, PipeModel meta)
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

        public async Task<bool> Predict(AgentBase agent, NlpDoc doc, PipeModel meta)
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
                Confidence = result.First().Item2
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
                    ModelName = meta.Model,
                    Word2VecFilePath = Configuration.GetValue<string>("wordvecModel")
                };

                if (!String.IsNullOrEmpty(options.Word2VecFilePath))
                {
                    string contentDir = AppDomain.CurrentDomain.GetData("DataPath").ToString();
                    options.Word2VecFilePath = options.Word2VecFilePath.Replace("|App_Data|", contentDir + System.IO.Path.DirectorySeparatorChar);
                }

                _classifier = new ClassifierFactory<SentenceFeatureExtractor>(options, SupportedLanguage.English);

                string classifierName = Configuration.GetValue<String>($"classifer");

                _classifier.GetClassifer(classifierName);
            }
        }
    }
}
