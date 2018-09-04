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

        public void Classify(Sentence sentence)
        {
            
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
            var features = new List<Feature>();

            features.Add(new Feature("StartsWith(A)", words[0].Text.StartsWith("A").ToString()));
            features.Add(new Feature("EndsWith(a)", words[0].Text.EndsWith("a").ToString()));

            return features;
        }
    }
}
