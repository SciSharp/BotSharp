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

            var options = new ClassifyOptions
            {
                ModelFilePath = modelFileName
            };
            var classifier = new ClassifierFactory<NaiveBayesClassifier, SentenceFeatureExtractor>(options, SupportedLanguage.English);

            var sentences = doc.Sentences.Select(x => new Sentence
            {
                Label = x.Intent.Label,
                Text = x.Text,
                Words = x.Tokens
            }).ToList();

            classifier.Train(sentences);

            Console.WriteLine($"Saved model to {modelFileName}");

            return true;
        }

        public async Task<bool> Predict(Agent agent, NlpDoc doc, PipeModel meta)
        {
            var options = new ClassifyOptions
            {
                ModelFilePath = Path.Combine(Settings.ModelDir, meta.Model)
            };
            var classifier = new ClassifierFactory<NaiveBayesClassifier, SentenceFeatureExtractor>(options, SupportedLanguage.English);

            var sentence = doc.Sentences.Select(s => new Sentence
            {
                Text = s.Text,
                Words = s.Tokens
            }).First();


            var result = classifier.Classify(sentence);

            doc.Sentences[0].Intent = new TextClassificationResult
            {
                Classifier = "BotSharpNBayesClassifier",
                Label = result.First().Item1,
                Confidence = (decimal)result.First().Item2
            };

            return true;
        }
    }
}
