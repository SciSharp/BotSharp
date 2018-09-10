using BotSharp.Algorithm.Features;
using BotSharp.NLP.Tokenize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.NLP.Classify
{
    public class ClassifierFactory<IClassify, IFeatureExtractor> 
        where IClassify : IClassifier, new() 
        where IFeatureExtractor : ITextFeatureExtractor, new()
    {
        private SupportedLanguage _lang;

        private IClassify _classifier;

        private ClassifyOptions _options;

        private IFeatureExtractor featureExtractor;

        public ClassifierFactory(ClassifyOptions options, SupportedLanguage lang)
        {
            _lang = lang;
            _options = options;
            _classifier = new IClassify();
            featureExtractor = new IFeatureExtractor();
        }

        public List<Tuple<string, double>> Classify(Sentence sentence)
        {
            var classes = _classifier.Classify(featureExtractor.GetFeatures(sentence.Words), new ClassifyOptions
            {
            });

            return classes.OrderByDescending(x => x.Item2).ToList();
        }

        public void Train(List<Sentence> sentences)
        {
            _classifier.Train(sentences.Select(x => new FeaturesWithLabel
            {
                Label = x.Label,
                Features = featureExtractor.GetFeatures(x.Words)
            }).ToList(), _options);
        }
    }
}
