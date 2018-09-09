using BotSharp.NLP.Corpus;
using BotSharp.NLP.Tokenize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.NLP.Classify
{
    public class ClassifierFactory<IClassify> where IClassify : IClassifier, new()
    {
        private SupportedLanguage _lang;

        private IClassify _classifier;

        private ClassifyOptions _options;

        public ClassifierFactory(ClassifyOptions options, SupportedLanguage lang)
        {
            _lang = lang;
            _options = options;
            _classifier = new IClassify();
        }

        public List<Tuple<string, double>> Classify(Sentence sentence)
        {
            var classes = _classifier.Classify(new LabeledFeatureSet
            {
                Features = GetFeatures(sentence.Words)
            }, new ClassifyOptions
            {
            });

            return classes.OrderByDescending(x => x.Item2).ToList();
        }

        public void Train(List<Sentence> sentences)
        {
            _classifier.Train(sentences.Select(x => new LabeledFeatureSet
            {
                Label = x.Label,
                Features = GetFeatures(x.Words)
            }).ToList(), _options);
        }

        private List<Feature> GetFeatures(List<Token> words)
        {
            string text = words[0].Text;
            var features = new List<Feature>();

            features.Add(new Feature("alwayson", "True"));
            features.Add(new Feature("startswith", text[0].ToString().ToLower()));
            features.Add(new Feature("endswith", text[text.Length - 1].ToString().ToLower()));

            return features;
        }
    }
}
